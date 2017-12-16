using System;

namespace brewino.net
{
    public class BreweryControllerSettings : ISettings
    {
        #region ISettings implementation
        public string Filename { get { return "brewery.json"; } }
        #endregion

        public double MashKp { get; set; }
        public double MashKi { get; set; }
        public double MashKd { get; set; }
        public double BoilKp { get; set; }
        public double BoilKi { get; set; }
        public double BoilKd { get; set; }

    }

    public class BreweryController
    {
        public readonly PidController MashTempController = new PidController();

        public readonly PidController BoilTempController = new PidController();

        public BreweryController()
        {
        }

        /*
        public async Task<bool> Reset()
        {
            
        }
        */

    }
}

