namespace ProductManagementSystem.Api.Helpers;

public static class EmailContentHelper
{
    public static string GetMailContent(string fileName, Dictionary<string, string> parameters)
    {
        if(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles", fileName)))
        {
            string fileContent = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles", fileName));

            foreach(var item in parameters)
            {
                string keyToSearch = string.Concat("{{", item.Key.ToString(), "}}");

                fileContent = fileContent.Replace(keyToSearch, item.Value);
            }

            return fileContent;
        }
        else
        {
            return "";
        }
    }
}
