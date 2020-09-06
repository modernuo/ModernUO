namespace Server
{
    public static class Application
    {
        public static void Main(string[] args)
        {
            Core.Setup(args);
            Core.Run();
        }
    }
}
