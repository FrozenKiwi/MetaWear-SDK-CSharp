using System.Threading.Tasks;

namespace MbientLab.MetaWear {
    /// <summary>
    /// Data producer that emits data only when new data is available.  
    /// </summary>
    public interface IAsyncDataProducer : IDataProducer {
        /// <summary>
        /// Begin data collection
        /// </summary>
        Task Start();
        /// <summary>
        /// End data collection
        /// </summary>
        Task Stop();
    }
}
