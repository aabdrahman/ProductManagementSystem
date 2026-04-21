using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProductManagementSystem.Api.Entities.ChannelBrokers;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.MailOperation;
using Serilog;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ProductManagementSystem.Api.Utilities;

public class EmailService : IEmailService
{
    private readonly IFluentEmailFactory _fluentEmailFactory;
    private readonly EmailSettingsConfig _emailSettingsConfig;
    private readonly Channel<SendPriorityMailEvent> _sendPriorityMailChannel;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    private ConcurrentQueue<EmailSenderDto> _queuedEmails = new();
    private ConcurrentQueue<EmailSenderDto> _tempQueuedEmails = new();
    private bool _isProcessingQueuedEmail = false;
    private bool _isPerformingOperationOnQueuedItems = false;

    public EmailService(IServiceScopeFactory serviceScopeFactory, IOptionsMonitor<EmailSettingsConfig> emailSettingsOptionsMonitor, Channel<SendPriorityMailEvent> sendPriorityMailChannel)
    {
        _fluentEmailFactory = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IFluentEmailFactory>();
        _emailSettingsConfig = emailSettingsOptionsMonitor.CurrentValue;
        _sendPriorityMailChannel = sendPriorityMailChannel;
    }

    public Task<(int tempQueuedCount, int queuedCount)> GetPriorityMails()
    {
        try
        {
            return Task.FromResult((_tempQueuedEmails.Count, _queuedEmails.Count));
        }
        catch (Exception ex)
        {
            return Task.FromResult((0, 0));
        }
    }

    public async Task<ProcessedMailResultDto> ProcessPriorityMails()
    {
        try
        {
            Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessPriorityMails)).Information("Begin Processing high priority mails...");

            int successCount = 0;
            int failureCount = 0;
            int totalProcessed = 0;

            //if(!(_sendPriorityMailChannel.Reader.Count > 0))
            //{
            //    Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessPriorityMails)).Information("No priority mail is available to process...");
            //    return new ProcessedMailResultDto(totalProcessed, successCount, failureCount);
            //}

            await foreach (var priorityMail in _sendPriorityMailChannel.Reader.ReadAllAsync())
            {
                try
                {
                    Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessPriorityMails)).Information("Processing high priority mail - {0}", priorityMail);

                    SendResponse sendMailResult = await _fluentEmailFactory.Create()
                                                    .To(priorityMail.EmailDetails.Recipients.Select(x => new Address(x)))
                                                    .Subject(priorityMail.EmailDetails.Subject)
                                                    .Body(priorityMail.EmailDetails.Content, isHtml: priorityMail.EmailDetails.isHtml)
                                                    .SendAsync();
                    if(sendMailResult.Successful)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                        Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessPriorityMails)).Information("High Priority Email processing failed - {0}. Send Mail Response - {1}", priorityMail.EmailDetails, sendMailResult.ErrorMessages);
                    }
                }
                catch (Exception ex)
                {
                    Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessPriorityMails)).Error(ex, "High Priority Mail could not be processed. Queuing back to normal mails.");

                    _queuedEmails.Enqueue(priorityMail.EmailDetails);

                    failureCount++;
                }
                finally
                {
                    totalProcessed++;
                }
            }

            return new ProcessedMailResultDto(totalProcessed, successCount, failureCount);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessPriorityMails)).Error(ex, "An Error Occurred while sending queued high priorty mail.");
            return new ProcessedMailResultDto(0, 0, 0);
        }
    }

    public async Task<ProcessedMailResultDto> ProcessQueuedEmails(bool processAll = false)
    {
        try
        {
            Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessQueuedEmails)).Information("Begin Processing Queued Emails.......");

            if (processAll)
            {
                if(_queuedEmails.Count > 0)
                {
                    int totalProcessedRecords = 0;
                    int successCount = 0; int failureCount = 0;


                    while (_queuedEmails.Count > 0)
                    {
                        EmailSenderDto emailToProcess;
                        try
                        {
                            if (_queuedEmails.TryDequeue(out emailToProcess))
                            {
                                IEnumerable<Address> recipientsAddress = emailToProcess.Recipients.Select(x => new FluentEmail.Core.Models.Address(x)).ToList();

                                SendResponse result = await _fluentEmailFactory.Create()
                                                    .Subject(emailToProcess.Subject)
                                                    .To(mailAddresses: recipientsAddress)
                                                    .Body(emailToProcess.Content, isHtml: emailToProcess.isHtml)
                                                    .SendAsync();

                                if (result.Successful)
                                {

                                    successCount++;
                                }
                                else
                                {
                                    _tempQueuedEmails.Enqueue(emailToProcess);
                                    failureCount++;
                                }
                            }
                            else
                            {
                                Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessQueuedEmails)).Warning("Queued Email could not be fetched from store. Try Dequeue returns false.");
                                failureCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessQueuedEmails)).Error(ex, "An Error Occurred while sending queued mail.");
                            failureCount++;
                        }
                        finally
                        {
                            totalProcessedRecords++;
                        }
                    }

                    return new ProcessedMailResultDto(totalProcessedRecords, successCount, failureCount);
                }
                else
                {
                    ProcessedMailResultDto processedMailResult = new(0, 0, 0);

                    return processedMailResult;
                }
            }
            else
            {
                _isProcessingQueuedEmail = true;

                int totalProcessedRecords = 0;
                int successCount = 0; int failureCount = 0;

                if (_queuedEmails.Count > 0)
                {

                    while (totalProcessedRecords < _emailSettingsConfig.TotalBatchSize && _queuedEmails.Count > 0)
                    {
                        EmailSenderDto fetchedEmail;
                        try
                        {
                            if (_queuedEmails.TryDequeue(out fetchedEmail))
                            {
                                IEnumerable<Address> recipientsAddress = fetchedEmail.Recipients.Select(x => new FluentEmail.Core.Models.Address(x)).ToList();

                                try
                                {
                                    SendResponse result = await _fluentEmailFactory.Create()
                                                    .Subject(fetchedEmail.Subject)
                                                    .To(mailAddresses: recipientsAddress)
                                                    .Body(fetchedEmail.Content, isHtml: fetchedEmail.isHtml)
                                                    .SendAsync();

                                    if (result.Successful)
                                    {
                                        successCount++;
                                    }
                                    else
                                    {
                                        _tempQueuedEmails.Enqueue(fetchedEmail);
                                        failureCount++;
                                    }

                                }
                                catch (Exception ex)
                                {
                                    Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessQueuedEmails)).Warning("Queued Email could not be fetched from store. Try Dequeue returns false.");
                                    _tempQueuedEmails.Enqueue(fetchedEmail);
                                    failureCount++;
                                }
                                
                            }
                            else
                            {
                                Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessQueuedEmails)).Warning("Queued Email could not be fetched from store. Try Dequeue returns false.");
                                _tempQueuedEmails.Enqueue(fetchedEmail);
                                failureCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            //_tempQueuedEmails.Enqueue(fetchedEmail);
                            Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessQueuedEmails)).Error(ex, "An Error Occurred while sending queued mail.");
                            failureCount++;
                        }
                        finally
                        {
                            totalProcessedRecords++;
                        }
                    }

                    return new ProcessedMailResultDto(totalProcessedRecords, successCount, failureCount);
                }
                else
                {
                    ProcessedMailResultDto processedMailResult = new(0, 0, 0);

                    return processedMailResult;

                }
            }
            

        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(ProcessQueuedEmails)).Error(ex, "An Error Occurred while sending queued mail.");
            return new ProcessedMailResultDto(0, 0, 0);
        }
        finally
        {
            _isProcessingQueuedEmail = false;

            while(_tempQueuedEmails.Count > 0)
            {
                bool isTempQueued = _tempQueuedEmails.TryDequeue(out EmailSenderDto? tempToQueue);

                if(isTempQueued)
                {
                    _queuedEmails.Enqueue(tempToQueue!);
                }
            }
        }
    }

    [NonAction]
    public async Task<bool> RevokeEmailAsync(EmailSenderDto emailToRevoke)
    {
        try
        {
            Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(RevokeEmailAsync)).Information("Revoke Email - {0}}", emailToRevoke);

            await Task.Delay(TimeSpan.FromMilliseconds(1));

            if (_isProcessingQueuedEmail)
            {


                return false;
            }

            _isPerformingOperationOnQueuedItems = true;

            EmailSenderDto[] queuedEmailAsArray = _queuedEmails.ToArray();

            int position = Array.IndexOf(queuedEmailAsArray, emailToRevoke);

            return true;

        }
        catch (Exception ex)
        {

            _isPerformingOperationOnQueuedItems = false;

            return false;
            throw;
        }
    }

    public async Task<bool> SendEmailAsync(EmailSenderDto emailToSend)
    {
        try
        {
            Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(SendEmailAsync)).Information("Queueing Email At: {0} - {1}", DateTime.UtcNow.ToLocalTime(), emailToSend);

            await Task.Delay(TimeSpan.FromMilliseconds(1));

            if(emailToSend.priority == EmailPriority.Low || emailToSend.priority == EmailPriority.Medium)
            {
                if (_isProcessingQueuedEmail || _isPerformingOperationOnQueuedItems)
                {
                    _tempQueuedEmails.Enqueue(emailToSend);
                    Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(SendEmailAsync)).Information("Email Temporarily Queued At: {0}. Current Total Queued - {1}", DateTime.UtcNow.ToLocalTime(), _tempQueuedEmails.Count);
                }
                else
                {
                    _queuedEmails.Enqueue(emailToSend);
                    Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(SendEmailAsync)).Information("Email Queued At: {0}. Current Total Queued - {1}", DateTime.UtcNow.ToLocalTime(), _queuedEmails.Count);
                }

                return true;
            }
            else
            {
                if(await _sendPriorityMailChannel.Writer.WaitToWriteAsync())
                {
                   await  _sendPriorityMailChannel.Writer.WriteAsync(new SendPriorityMailEvent(emailToSend));
                }
                else
                {
                    _queuedEmails.Enqueue(emailToSend);
                }

                return true;
            }
            
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(EmailService)).ForContext(_methodName, nameof(SendEmailAsync)).Error(ex, "An Error Occurred while queueing email");
            return false;
        }
    }
}
