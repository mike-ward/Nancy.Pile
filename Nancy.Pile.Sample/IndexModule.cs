namespace Nancy.Pile.Sample
{
    using Nancy;

    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Get["/"] = parameters => View["index"];
        }
    }
}