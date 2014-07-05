namespace Nancy.PIle.Sample.Owin
{
    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Get["/"] = p => View["Index"];
        }
    }
}