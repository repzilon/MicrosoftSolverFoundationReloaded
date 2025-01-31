using System;
using System.Globalization;
using System.Reflection;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>Solver Foundation license and SKU information.
	/// </summary>
	internal static class License
	{
		/// <summary>SKU information.
		/// </summary>
		public static string Sku = Resources.VersionExpress;

		/// <summary>Nonzero limit
		/// </summary>
		public static int NonzeroLimit = 50000;

		/// <summary>Variable limit
		/// </summary>
		public static int VariableLimit;

		/// <summary>MIP variable limit.
		/// </summary>
		public static int MipVariableLimit = 1000;

		/// <summary>MIP row limit.
		/// </summary>
		public static int MipRowLimit = 1000;

		/// <summary>MIP nonzero limit.
		/// </summary>
		public static int MipNonzeroLimit = 5000;

		/// <summary>CSP term count limit.
		/// </summary>
		public static int CspTermLimit = 5000;

		/// <summary>Expiration of evaluation period (if any).
		/// </summary>
		public static DateTime? Expiration = null;

		public static string VersionToString()
		{
			return string.Format(CultureInfo.CurrentCulture, Resources.MicrosoftSolverFoundationVersion01, new object[2]
			{
				GetSolverFoundationAssemblyVersion(),
				Sku
			});
		}

		internal static string GetSolverFoundationAssemblyVersion()
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			if (executingAssembly != null)
			{
				AssemblyName name = executingAssembly.GetName();
				if (name != null)
				{
					return name.Version.ToString();
				}
			}
			return "??";
		}

		internal static string LimitsToString()
		{
			return string.Format(CultureInfo.CurrentCulture, Resources.LicenseFormat0123456, VariableLimit, NonzeroLimit, MipVariableLimit, MipRowLimit, MipNonzeroLimit, CspTermLimit, Expiration.HasValue ? Expiration.ToString() : Resources.NoExpiration);
		}
	}
}
