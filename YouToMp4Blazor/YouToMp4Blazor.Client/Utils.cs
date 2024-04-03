namespace YouToMp4Blazor.Client;

public static class Utils
{
    public static void WriteLine( Exception e )
    {
        Console.Error.WriteLine( $"{e} : {e.Message}" );
    }
}