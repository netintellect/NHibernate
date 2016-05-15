using System;
using Newtonsoft.Json;
using VLM.DAS2.Model.Entities.Core;

namespace VLM.DAS2.Model.Entities.Dossiers
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Dossier : ValidatedEntity<Dossier>
    {
        #region state
        private int _dossierNummer;
        [JsonProperty]
        public int DossierNummer
        {
            get { return _dossierNummer; }
            set { SetProperty(value, ref _dossierNummer, () => DossierNummer); }
        }

        private DateTime _aanslagJaar;
        [JsonProperty]
        public DateTime AanslagJaar
        {
            get { return _aanslagJaar; }
            set { SetProperty(value, ref _aanslagJaar, () => AanslagJaar); }
        }

        private DateTime _productieJaar;
        [JsonProperty]
        public DateTime ProductieJaar
        {
            get { return _productieJaar; }
            set { SetProperty(value, ref _productieJaar, () => ProductieJaar); }
        }

        private string _opmerking;
        [JsonProperty]
        public string Opmerking
        {
            get { return _opmerking; }
            set { SetProperty(value, ref _opmerking, () => Opmerking); }
        }
        #endregion
    }
}
