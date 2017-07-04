using MbientLab.MetaWear.Impl;

using NUnit.Framework;
using System;

namespace MbientLab.MetaWear.Test {
    internal abstract class UnitTestBase {
        protected MetaWearBoard metawear;
        protected NunitPlatform platform;

        protected UnitTestBase(params Type[] types) {
            platform = new NunitPlatform(new InitializeResponse(types));
        }

        protected UnitTestBase(Model model) {
            platform = new NunitPlatform(new InitializeResponse(model));
        }

        [TearDown]
        public virtual void Cleanup() {
            platform.Reset();
        }

        [SetUp]
        public virtual void SetUp() {
            metawear = new MetaWearBoard(platform, platform);
            metawear.InitializeAsync().Wait();
        }
    }
}
