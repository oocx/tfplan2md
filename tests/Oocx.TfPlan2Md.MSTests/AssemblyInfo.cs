// Assembly-level test configuration for MSTest
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Enable parallel test execution at the class level for better performance
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]
