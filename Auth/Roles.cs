namespace SafeScribe.Api.Auth;

public static class Roles
{
    public const string Leitor = "Leitor";
    public const string Editor = "Editor";
    public const string Admin  = "Admin";

 
    public const string EditorOuAdmin = Editor + "," + Admin;
}
