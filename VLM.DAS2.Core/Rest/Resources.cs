namespace VLM.DAS2.Core.Rest
{
    public static class Routings
    {
        public const string Filter = "/filter";

        public static class DossierBeheer
        {
            public const string Root = "api/dossierbeheer";
            
            public static class Dossier
            {
                public const string Us = "Dossiers";
                public const string Self = "Dossier";
                public const string Filter = Us + Routings.Filter;
            }
        }
        
        public static class Logs
        {
            public const string Root = "api/logs";

            public static class ErrorLog
            {
                public const string GetErrorLogs = "error";
            }
        }

    }

    public static class Resources
    {
        public static readonly string ParamOne = "|parameter|";
        public static readonly string ParamTwo = "|parameter2|";

        public static class DossierBeheer
        {
            public static class Dossier
            {
                public static readonly string Self =
                    $"{Routings.DossierBeheer.Root}/{Routings.DossierBeheer.Dossier.Self}";

                public static readonly string Filter =
                    $"{Routings.DossierBeheer.Root}/{Routings.DossierBeheer.Dossier.Filter}";
            }
        }

        public static class Logs
        {
            public static class ErrorLog
            {
                public static readonly string GetErrorLogs =
                    $"{Routings.Logs.Root}/{Routings.Logs.ErrorLog.GetErrorLogs}";
            }
        }
    }
}
