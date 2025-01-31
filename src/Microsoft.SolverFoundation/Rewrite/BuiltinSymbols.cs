namespace Microsoft.SolverFoundation.Rewrite
{
	internal class BuiltinSymbols
	{
		protected RewriteSystem _rs;

		public readonly RootSymbol Root;

		public readonly Symbol Null;

		public readonly Symbol Failed;

		public readonly AttributesSymbol Attributes;

		public readonly AddAttributesSymbol AddAttributes;

		public readonly RemoveAttributesSymbol RemoveAttributes;

		public readonly ClearAttributesSymbol ClearAttributes;

		public readonly IntegerSymbol Integer;

		public readonly RationalSymbol Rational;

		public readonly FloatSymbol Float;

		public readonly BooleanSymbol Boolean;

		public readonly StringSymbol String;

		public readonly ClrObjectSymbol ClrObject;

		public readonly SequenceSymbol Sequence;

		public readonly HoleSymbol Hole;

		public readonly HoleSpliceSymbol HoleSplice;

		public readonly SlotSymbol Slot;

		public readonly SlotSpliceSymbol SlotSplice;

		public readonly ArgumentSpliceSymbol ArgumentSplice;

		public readonly AnnotationSymbol Annotation;

		public readonly ListSymbol List;

		public readonly EvaluateToLastSymbol EvaluateToLast;

		public readonly EvaluateToFirstSymbol EvaluateToFirst;

		public readonly HoldPatternSymbol HoldPattern;

		public readonly HoldSymbol Hold;

		public readonly EvaluateSymbol Evaluate;

		public readonly RuleSymbol Rule;

		public readonly RuleDelayedSymbol RuleDelayed;

		public readonly SetSymbol Set;

		public readonly SetDelayedSymbol SetDelayed;

		public readonly UnsetSymbol Unset;

		public readonly ClearValuesSymbol ClearValues;

		public readonly ClearAllSymbol ClearAll;

		public readonly DownValuesSymbol DownValues;

		public readonly ConditionSymbol Condition;

		public readonly PatternSymbol Pattern;

		public readonly FunctionSymbol Function;

		public readonly ModuleSymbol Module;

		public readonly ReplaceSymbol Replace;

		public readonly ReplaceAllSymbol ReplaceAll;

		public readonly ReplaceRepeatedSymbol ReplaceRepeated;

		public readonly FilterSymbol Filter;

		public readonly SelectSymbol Select;

		public readonly HeadSymbol Head;

		public readonly PartSymbol Part;

		public readonly LengthSymbol Length;

		public readonly ApplySymbol Apply;

		public readonly MapSymbol Map;

		public readonly ThreadSymbol Thread;

		public readonly FullFormSymbol FullForm;

		public readonly OrderSymbol Order;

		public readonly SortSymbol Sort;

		public readonly FreeOfSymbol FreeOf;

		public readonly IdenticalSymbol Identical;

		public readonly UnIdenticalSymbol UnIdentical;

		public readonly PlusSymbol Plus;

		public readonly TimesSymbol Times;

		public readonly QuotientSymbol Quotient;

		public readonly QuotientTruncSymbol QuotientTrunc;

		public readonly ModSymbol Mod;

		public readonly ModTruncSymbol ModTrunc;

		public readonly MinusSymbol Minus;

		public readonly PowerSymbol Power;

		public readonly AbsSymbol Abs;

		public readonly FactorialSymbol Factorial;

		public readonly IntegerPartSymbol IntegerPart;

		public readonly FractionalPartSymbol FractionalPart;

		public readonly FloorSymbol Floor;

		public readonly CeilingSymbol Ceiling;

		public readonly MaxSymbol Max;

		public readonly MinSymbol Min;

		public readonly CosSymbol Cos;

		public readonly SinSymbol Sin;

		public readonly TanSymbol Tan;

		public readonly ArcCosSymbol ArcCos;

		public readonly ArcSinSymbol ArcSin;

		public readonly ArcTanSymbol ArcTan;

		public readonly CoshSymbol Cosh;

		public readonly SinhSymbol Sinh;

		public readonly TanhSymbol Tanh;

		public readonly ExpSymbol Exp;

		public readonly LogSymbol Log;

		public readonly Log10Symbol Log10;

		public readonly SqrtSymbol Sqrt;

		public readonly IfSymbol If;

		public readonly NotSymbol Not;

		public readonly OrSymbol Or;

		public readonly AndSymbol And;

		public readonly XorSymbol Xor;

		public readonly ImpliesSymbol Implies;

		public readonly OrElseSymbol OrElse;

		public readonly AndAlsoSymbol AndAlso;

		public readonly AsIntSymbol AsInt;

		public readonly EqualSymbol Equal;

		public readonly UnequalSymbol Unequal;

		public readonly LessSymbol Less;

		public readonly LessEqualSymbol LessEqual;

		public readonly GreaterSymbol Greater;

		public readonly GreaterEqualSymbol GreaterEqual;

		public readonly InequalitySymbol Inequality;

		public readonly DoSymbol Do;

		public readonly TableSymbol Table;

		public readonly ForeachSymbol Foreach;

		public readonly FilteredForeachSymbol FilteredForeach;

		public readonly GenerateSymbol Generate;

		public readonly SumSymbol Sum;

		public readonly FilteredSumSymbol FilteredSum;

		public readonly ArraySymbol Array;

		public readonly OuterSymbol Outer;

		public readonly ExcelOutputsymbol BindOut;

		public readonly InSymbol In;

		public readonly InOrSymbol InOr;

		public readonly CacheSequenceSymbol CacheSequence;

		public readonly RealizeSequenceSymbol RealizeSequence;

		public readonly RealizeSequenceSymbol SpliceSequence;

		public readonly ConcatSequenceSymbol ConcatSequence;

		public readonly StringJoinSymbol StringJoin;

		public readonly StringLengthSymbol StringLength;

		public readonly UniformDistributionSymbol UniformDistribution;

		public readonly NormalDistributionSymbol NormalDistribution;

		public readonly DiscreteUniformDistributionSymbol DiscreteUniformDistribution;

		public readonly GeometricDistributionSymbol GeometricDistribution;

		public readonly ExponentialDistributionSymbol ExponentialDistribution;

		public readonly BinomialDistributionSymbol BinomialDistribution;

		public readonly LogNormalDistributionSymbol LogNormalDistribution;

		public readonly DecisionsSymbol Decisions;

		public readonly DomainsSymbol Domains;

		public readonly ParametersSymbol Parameters;

		public readonly InputSectionSymbol InputSection;

		public readonly RealsSymbol Reals;

		public readonly IntegersSymbol Integers;

		public readonly EnumSymbol Enum;

		public readonly BooleansSymbol Booleans;

		public readonly AnySymbol Any;

		public readonly ScenariosSymbol Scenarios;

		public readonly ConstraintsSymbol Constraints;

		public readonly MaximizeSymbol Maximize;

		public readonly MinimizeSymbol Minimize;

		public readonly GoalsSymbol Goals;

		public readonly ModelSymbol Model;

		public readonly TupleSymbol Tuple;

		public readonly SetsSymbol Sets;

		public readonly RecourseSymbol Recourse;

		public readonly TuplesSymbol Tuples;

		public readonly ExcelInputTypeSymbol ExcelInputType;

		public readonly InputSymbol BindIn;

		public readonly Sos1Symbol Sos1;

		public readonly Sos2Symbol Sos2;

		public readonly ElementOfSymbol ElementOf;

		public readonly GetClrTypeSymbol GetClrType;

		public readonly GetClrFieldSymbol GetClrField;

		public readonly GetClrPropertySymbol GetClrProperty;

		public readonly InvokeClrMethodSymbol InvokeClrMethod;

		public readonly CreateClrObjectSymbol CreateClrObject;

		protected internal BuiltinSymbols(RewriteSystem rs)
		{
			_rs = rs;
			Root = new RootSymbol(_rs);
			Null = new Symbol(_rs, "Null");
			Failed = new Symbol(_rs, "Failed");
			Attributes = new AttributesSymbol(_rs);
			AddAttributes = new AddAttributesSymbol(_rs);
			RemoveAttributes = new RemoveAttributesSymbol(_rs);
			ClearAttributes = new ClearAttributesSymbol(_rs);
			Integer = new IntegerSymbol(_rs);
			Rational = new RationalSymbol(_rs);
			Float = new FloatSymbol(_rs);
			Boolean = new BooleanSymbol(_rs);
			String = new StringSymbol(_rs);
			ClrObject = new ClrObjectSymbol(_rs);
			Sequence = new SequenceSymbol(_rs);
			Hole = new HoleSymbol(_rs);
			HoleSplice = new HoleSpliceSymbol(_rs);
			Slot = new SlotSymbol(_rs);
			SlotSplice = new SlotSpliceSymbol(_rs);
			ArgumentSplice = new ArgumentSpliceSymbol(_rs);
			Annotation = new AnnotationSymbol(_rs);
			List = new ListSymbol(_rs);
			EvaluateToLast = new EvaluateToLastSymbol(_rs);
			EvaluateToFirst = new EvaluateToFirstSymbol(_rs);
			HoldPattern = new HoldPatternSymbol(_rs);
			Hold = new HoldSymbol(_rs);
			Evaluate = new EvaluateSymbol(_rs);
			Rule = new RuleSymbol(_rs);
			RuleDelayed = new RuleDelayedSymbol(_rs);
			Set = new SetSymbol(_rs);
			SetDelayed = new SetDelayedSymbol(_rs);
			Unset = new UnsetSymbol(_rs);
			ClearValues = new ClearValuesSymbol(_rs);
			ClearAll = new ClearAllSymbol(_rs);
			DownValues = new DownValuesSymbol(_rs);
			Condition = new ConditionSymbol(_rs);
			Pattern = new PatternSymbol(_rs);
			Function = new FunctionSymbol(_rs);
			Module = new ModuleSymbol(_rs);
			Replace = new ReplaceSymbol(_rs);
			ReplaceAll = new ReplaceAllSymbol(_rs);
			ReplaceRepeated = new ReplaceRepeatedSymbol(_rs);
			Filter = new FilterSymbol(_rs);
			Select = new SelectSymbol(_rs);
			Head = new HeadSymbol(_rs);
			Part = new PartSymbol(_rs);
			Length = new LengthSymbol(_rs);
			Apply = new ApplySymbol(_rs);
			Map = new MapSymbol(_rs);
			Thread = new ThreadSymbol(_rs);
			FullForm = new FullFormSymbol(_rs);
			Order = new OrderSymbol(_rs);
			Sort = new SortSymbol(_rs);
			FreeOf = new FreeOfSymbol(_rs);
			Identical = new IdenticalSymbol(_rs);
			UnIdentical = new UnIdenticalSymbol(_rs);
			Plus = new PlusSymbol(_rs);
			Times = new TimesSymbol(_rs);
			Quotient = new QuotientSymbol(_rs);
			QuotientTrunc = new QuotientTruncSymbol(_rs);
			Mod = new ModSymbol(_rs);
			ModTrunc = new ModTruncSymbol(_rs);
			Minus = new MinusSymbol(_rs);
			Power = new PowerSymbol(_rs);
			Abs = new AbsSymbol(_rs);
			Factorial = new FactorialSymbol(_rs);
			IntegerPart = new IntegerPartSymbol(_rs);
			FractionalPart = new FractionalPartSymbol(_rs);
			Floor = new FloorSymbol(_rs);
			Ceiling = new CeilingSymbol(_rs);
			Max = new MaxSymbol(_rs);
			Min = new MinSymbol(_rs);
			Cos = new CosSymbol(_rs);
			Sin = new SinSymbol(_rs);
			Tan = new TanSymbol(_rs);
			ArcCos = new ArcCosSymbol(_rs);
			ArcSin = new ArcSinSymbol(_rs);
			ArcTan = new ArcTanSymbol(_rs);
			Cosh = new CoshSymbol(_rs);
			Sinh = new SinhSymbol(_rs);
			Tanh = new TanhSymbol(_rs);
			Exp = new ExpSymbol(_rs);
			Log = new LogSymbol(_rs);
			Log10 = new Log10Symbol(_rs);
			Sqrt = new SqrtSymbol(_rs);
			If = new IfSymbol(_rs);
			Not = new NotSymbol(_rs);
			Or = new OrSymbol(_rs);
			And = new AndSymbol(_rs);
			Xor = new XorSymbol(_rs);
			Implies = new ImpliesSymbol(_rs);
			OrElse = new OrElseSymbol(_rs);
			AndAlso = new AndAlsoSymbol(_rs);
			AsInt = new AsIntSymbol(_rs);
			Equal = new EqualSymbol(_rs);
			Unequal = new UnequalSymbol(_rs);
			Less = new LessSymbol(_rs);
			LessEqual = new LessEqualSymbol(_rs);
			Greater = new GreaterSymbol(_rs);
			GreaterEqual = new GreaterEqualSymbol(_rs);
			Inequality = new InequalitySymbol(_rs);
			Do = new DoSymbol(_rs);
			Table = new TableSymbol(_rs);
			Foreach = new ForeachSymbol(_rs);
			FilteredForeach = new FilteredForeachSymbol(_rs);
			Generate = new GenerateSymbol(_rs);
			Sum = new SumSymbol(_rs);
			FilteredSum = new FilteredSumSymbol(_rs);
			Array = new ArraySymbol(_rs);
			Outer = new OuterSymbol(_rs);
			BindOut = new ExcelOutputsymbol(_rs);
			In = new InSymbol(_rs);
			InOr = new InOrSymbol(_rs);
			CacheSequence = new CacheSequenceSymbol(_rs);
			RealizeSequence = new RealizeSequenceSymbol(_rs, fSplice: false);
			SpliceSequence = new RealizeSequenceSymbol(_rs, fSplice: true);
			ConcatSequence = new ConcatSequenceSymbol(_rs);
			StringJoin = new StringJoinSymbol(_rs);
			StringLength = new StringLengthSymbol(_rs);
			UniformDistribution = new UniformDistributionSymbol(_rs);
			NormalDistribution = new NormalDistributionSymbol(_rs);
			DiscreteUniformDistribution = new DiscreteUniformDistributionSymbol(_rs);
			ExponentialDistribution = new ExponentialDistributionSymbol(_rs);
			GeometricDistribution = new GeometricDistributionSymbol(_rs);
			LogNormalDistribution = new LogNormalDistributionSymbol(_rs);
			BinomialDistribution = new BinomialDistributionSymbol(_rs);
			Decisions = new DecisionsSymbol(_rs);
			Domains = new DomainsSymbol(_rs);
			Parameters = new ParametersSymbol(_rs);
			InputSection = new InputSectionSymbol(_rs);
			Reals = new RealsSymbol(_rs);
			Integers = new IntegersSymbol(_rs);
			Enum = new EnumSymbol(_rs);
			Booleans = new BooleansSymbol(_rs);
			Scenarios = new ScenariosSymbol(_rs);
			Any = new AnySymbol(_rs);
			Constraints = new ConstraintsSymbol(_rs);
			Maximize = new MaximizeSymbol(_rs);
			Minimize = new MinimizeSymbol(_rs);
			Goals = new GoalsSymbol(_rs);
			Model = new ModelSymbol(_rs);
			Tuple = new TupleSymbol(_rs);
			Sets = new SetsSymbol(_rs);
			Recourse = new RecourseSymbol(_rs);
			Tuples = new TuplesSymbol(_rs);
			ExcelInputType = new ExcelInputTypeSymbol(_rs);
			BindIn = new InputSymbol(_rs);
			Sos1 = new Sos1Symbol(_rs);
			Sos2 = new Sos2Symbol(_rs);
			ElementOf = new ElementOfSymbol(_rs);
			GetClrType = new GetClrTypeSymbol(_rs);
			GetClrField = new GetClrFieldSymbol(_rs);
			GetClrProperty = new GetClrPropertySymbol(_rs);
			InvokeClrMethod = new InvokeClrMethodSymbol(_rs);
			CreateClrObject = new CreateClrObjectSymbol(_rs);
		}
	}
}
