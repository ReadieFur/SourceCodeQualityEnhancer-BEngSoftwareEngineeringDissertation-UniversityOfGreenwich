#if NETCOREAPP
global using Microsoft.VisualStudio.TestTools.UnitTesting;
#elif NETSTANDARD
global using NUnit.Framework;
#endif
global using static ReadieFur.SourceAnalyzer.UnitTests.Helpers;
