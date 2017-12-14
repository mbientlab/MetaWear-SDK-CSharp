using MbientLab.MetaWear.Impl;

using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    internal abstract class UnitTestBase {
        internal MetaWearBoard metawear;
        internal NunitPlatform platform;
        internal readonly InitializeResponse response;

        private UnitTestBase(InitializeResponse response) {
            this.response = response;
            platform = new NunitPlatform(response);
        }
        protected UnitTestBase(params Type[] types) : this(new InitializeResponse(types)) {
        }

        protected UnitTestBase(Model model) : this(new InitializeResponse(model)) {
        }

        [TearDown]
        public virtual void Cleanup() {
            platform = new NunitPlatform(response);
        }

        [SetUp]
        public async virtual Task SetUp() {
            metawear = new MetaWearBoard(platform, platform);
            await metawear.InitializeAsync();
        }
    }
}
