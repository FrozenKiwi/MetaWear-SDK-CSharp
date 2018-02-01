using MbientLab.MetaWear.Sensor.BarometerBosch;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Sensor {
    /// <summary>
    /// Sensor on the BME280 environmental sensor measuring relative humidity
    /// </summary>
    public interface IHumidityBme280 : IModule {
        /// <summary>
        /// Data producer representing relative humidity
        /// </summary>
        IForcedDataProducer Percentage { get; }
        /// <summary>
        /// Configure the snsor with settings specific to the BME280 humidity sensor
        /// </summary>
        /// <param name="os">Oversampling mode, defaults to standard</param>
        Task Configure(Oversampling os = Oversampling.Standard);
    }
}
