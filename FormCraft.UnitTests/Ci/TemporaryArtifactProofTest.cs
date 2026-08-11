namespace FormCraft.UnitTests.Ci;

/// <summary>
/// TEMPORARY — reverted in the next commit. Exists only to make one CI run red on purpose, so #225's
/// claim can be verified rather than assumed: a green run proves nothing here, because the whole
/// defect is that the *failure* path skipped the upload.
/// </summary>
public class TemporaryArtifactProofTest
{
    [Fact]
    public void Deliberate_Failure_To_Prove_The_TestResults_Artifact_Survives_A_Red_Run()
    {
        "formcraft-225-artifact-proof".ShouldBe("this assertion is meant to fail");
    }
}
