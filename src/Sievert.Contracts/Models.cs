namespace Sievert.Contracts;

/// <summary>Model profilinin kamuya acik tanimi.</summary>
public sealed record ModelResponse(
    string Code,
    string DisplayName,
    string RepositoryIdentity,
    string Trainer,
    string MlPackage,
    int FeatureCount,
    string FeatureSchemaVersion,
    double TrainThreshold,
    bool IsCalibrated,
    string ModelCodeCommit,
    string ModelChecksum,
    IReadOnlyList<string> Limitations);
