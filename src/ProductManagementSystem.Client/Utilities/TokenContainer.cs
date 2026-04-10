namespace ProductManagementSystem.Client.Utilities;

public class TokenContainer
{
    private string _token;  

    public string GetToken()
    {
        return _token;
    }

    public void SetToken(string token)
    {

        _token = token;

    }
}
