using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tom;

namespace Tests
{
    [TestClass]
    public class Bootstrapper
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            // Caution: Encryption keys used to secure real data must be kept secret!
            Settings.Current = new Settings("146 95 226 18 207 68 184 136 14 23 128 171 228 224 243 161");
        }
    }
}
