using System.Runtime.CompilerServices;

// Internal constructors on TaskStepData / TaskSequenceSO let EditMode tests build
// sequences in code instead of through serialized assets.
[assembly: InternalsVisibleTo("TaskSequence.Tests")]
[assembly: InternalsVisibleTo("Tutorial.Tests")]
