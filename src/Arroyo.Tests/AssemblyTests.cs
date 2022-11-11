namespace Arroyo.Tests;

public sealed class AssemblyTests : ModuleTests
{
    [Theory]
    [InlineData("MD5", "92df6cbac70c4f20")]
    [InlineData("None", "cdee32564a292b68")]
    [InlineData("SHA1", "cdee32564a292b68")]
    [InlineData("SHA256", "562e1e09eb9971fd")]
    [InlineData("SHA384", "ab2af597a4481ce2")]
    [InlineData("SHA512", "27ee222242e9b8d0")]
    public void Assembly_PublicKey_PublicKeyToken_HashAlgorithm(string hashName, string expectedPublicKeyToken)
    {
        var location = GetType().Assembly.Location;
        var keyFile = Path.Join(Path.GetDirectoryName(location), "assets/test.snk");

        var source = $@"
            using System.Reflection;
            using System.Configuration.Assemblies;
            [assembly: AssemblyAlgorithmId(AssemblyHashAlgorithm.{hashName})]
        ";
        var compilation = MetadataFactory.CreateCompilation(source);
        var options = compilation.Options.WithCryptoKeyFile(keyFile)
                                         .WithStrongNameProvider(new DesktopStrongNameProvider());
        compilation = compilation.WithOptions(options);

        var assembly = compilation.ToMetadataAssembly();
        
        var expectedPublicKey =
            @"00 24 00 00 04 80 00 00 94 00 00 00 06 02 00 00
              00 24 00 00 52 53 41 31 00 04 00 00 01 00 01 00
              79 5e 64 47 8d 6c 42 17 ca e4 61 e6 86 50 3b a5
              8c e8 bf 6e f1 25 99 7d c8 81 2c da 41 ab 88 dd
              f4 f4 33 00 40 f9 4d 88 85 8e 8f 3d 51 10 78 1a
              68 a5 40 b5 b3 ff ce 0f a7 cc 39 9b 79 60 0e f1
              f8 e7 62 99 37 da 7f 6c b4 9b 73 31 f9 c4 44 32
              89 7b 37 9d fc 10 bc c0 81 20 a2 13 f2 eb c2 72
              39 07 dd 87 8a f6 a7 9f 85 ad 7e 45 af 2a 24 98
              07 f7 f2 bc d5 ca ef 06 5b 08 a4 37 e4 ca cc c3".ReplaceLineEndings("").Replace(" ", "");

        var actualPublicKey = Convert.ToHexString(assembly.Identity.PublicKey.AsSpan()).ToLower();
        var actualPublicKeyToken = Convert.ToHexString(assembly.Identity.PublicKeyToken.AsSpan()).ToLower();
        
        actualPublicKey.Should().Be(expectedPublicKey);
        actualPublicKeyToken.Should().Be(expectedPublicKeyToken);
        assembly.Identity.HashAlgorithm.ToString().Should().BeEquivalentTo(hashName);
    }

    [Fact]
    public void Assembly_CustomAttributes()
    {
        var source = @"
            [assembly: Some]
            class SomeAttribute : System.Attribute { }            
        ";

        var module = MetadataFactory.CreateAssembly(source);
        module.GetCustomAttributes().Should().ContainSingle(x => x.Constructor.ContainingType.GetDocumentationId() == "T:SomeAttribute");
    }

    protected override MetadataModule CreateModule(string source)
    {
        return MetadataFactory.CreateAssembly(source).MainModule;
    }
}