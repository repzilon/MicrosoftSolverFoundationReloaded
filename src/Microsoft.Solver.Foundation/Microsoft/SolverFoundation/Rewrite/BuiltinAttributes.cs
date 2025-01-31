namespace Microsoft.SolverFoundation.Rewrite
{
	internal class BuiltinAttributes
	{
		private RewriteSystem _rs;

		internal readonly Symbol ValuesLocked;

		internal readonly Symbol AttributesLocked;

		internal readonly Symbol Flat;

		internal readonly Symbol UnaryIdentity;

		internal readonly Symbol Orderless;

		internal readonly Symbol Listable;

		internal readonly Symbol HoldAll;

		internal readonly Symbol HoldFirst;

		internal readonly Symbol HoldRest;

		internal readonly Symbol HoldSplice;

		protected internal BuiltinAttributes(RewriteSystem rs)
		{
			_rs = rs;
			ValuesLocked = new Symbol(_rs, "ValuesLocked");
			AttributesLocked = new Symbol(_rs, "AttributesLocked");
			Flat = new Symbol(_rs, "Flat");
			UnaryIdentity = new Symbol(_rs, "UnaryIdentity");
			Orderless = new Symbol(_rs, "Orderless");
			Listable = new Symbol(_rs, "Listable");
			HoldAll = new Symbol(_rs, "HoldAll");
			HoldFirst = new Symbol(_rs, "HoldFirst");
			HoldRest = new Symbol(_rs, "HoldRest");
			HoldSplice = new Symbol(_rs, "HoldSplice");
		}
	}
}
