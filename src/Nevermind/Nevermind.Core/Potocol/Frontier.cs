using System.Threading;

namespace Nevermind.Core.Potocol
{
    public class Frontier : IEthereumRelease
    {
        private static IEthereumRelease _instance;

        private Frontier()
        {
        }

        public static IEthereumRelease Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new Frontier());
        
        public bool IsTimeAdjustmentPostOlympic => true;
        public bool AreJumpDestinationsUsed => false;
        public bool IsEip2Enabled => false;
        public bool IsEip7Enabled => false;
        public bool IsEip100Enabled => false;
        public bool IsEip140Enabled => false;
        public bool IsEip150Enabled => false;
        public bool IsEip155Enabled => false;
        public bool IsEip158Enabled => false;
        public bool IsEip160Enabled => false;
        public bool IsEip170Enabled => false;
        public bool IsEip196Enabled => false;
        public bool IsEip197Enabled => false;
        public bool IsEip198Enabled => false;
        public bool IsEip211Enabled => false;
        public bool IsEip214Enabled => false;
        public bool IsEip649Enabled => false;
        public bool IsEip658Enabled => false;
    }
}