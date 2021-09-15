using SecOpsSteward.Shared;
using System;

namespace SecOpsSteward.Tests.Core
{
    public static class TestValues
    {
        public static Guid SampleGuidA = Guid.Parse("c71ed577-489a-48f2-a1d8-f63d6e15c667");
        public static Guid SampleGuidB = Guid.Parse("82369712-6097-4a37-b1c7-3dfbddd5905f");
        public static Guid SampleGuidC = Guid.Parse("015b9e9a-ac97-460f-b3f2-66c0c31aba4b");

        public static ChimeraAgentIdentifier SampleAgent = new ChimeraAgentIdentifier(Guid.Parse("08fe80da-5c8d-4ef7-a7ea-a4fd0449d5d8"));
        public static ChimeraAgentIdentifier SampleAgent2 = new ChimeraAgentIdentifier(Guid.Parse("288610a1-41ad-4d06-af96-01a07b2b68a7"));
        public static ChimeraPackageIdentifier SamplePackage = new ChimeraPackageIdentifier(Guid.Parse("91ee4ff7-591e-48df-9141-06343c4f1e1a"));
        public static ChimeraPackageIdentifier SamplePackage2 = new ChimeraPackageIdentifier(Guid.Parse("0c07f959-9441-42ae-a0b6-f67d99324730"));
        public static ChimeraUserIdentifier SampleUser = new ChimeraUserIdentifier(Guid.Parse("1e264c61-07d7-475c-8b8d-a3f0f5c3ba19"));
        public static ChimeraUserIdentifier SampleUser2 = new ChimeraUserIdentifier(Guid.Parse("0071f72a-7942-4bee-b92b-9e9659f87f78"));


        public static byte[] GetByteArray(int bytes)
        {
            Random rnd = new Random((int)DateTime.Now.Ticks);
            byte[] b = new byte[bytes];
            rnd.NextBytes(b);
            return b;
        }
    }
}
