// This code was generated by the Gardens Point Parser Generator
// Copyright (c) Wayne Kelly, John Gough, QUT 2005-2014
// (see accompanying GPPGcopyright.rtf)

// GPPG version 1.5.2
// Machine:  TAL-PC
// DateTime: 11/7/2016 3:58:44 PM
// UserName: Tal
// Input file <Indexing\LuceneQuery.Language.grammar.y - 11/7/2016 3:57:54 PM>

// options: no-lines gplex

using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;
using QUT.Gppg;

namespace Raven.Database.Indexing
{
internal enum Token {error=2,EOF=3,NOT=4,OR=5,AND=6,
    INTERSECT=7,PLUS=8,MINUS=9,OPEN_CURLY_BRACKET=10,CLOSE_CURLY_BRACKET=11,OPEN_SQUARE_BRACKET=12,
    CLOSE_SQUARE_BRACKET=13,TILDA=14,BOOST=15,QUOTE=16,TO=17,COLON=18,
    OPEN_PAREN=19,CLOSE_PAREN=20,ALL_DOC=21,UNANALIZED_TERM=22,METHOD=23,UNQUOTED_TERM=24,
    QUOTED_TERM=25,QUOTED_WILDCARD_TERM=26,FLOAT_NUMBER=27,INT_NUMBER=28,DOUBLE_NUMBER=29,LONG_NUMBER=30,
    DATETIME=31,NULL=32,PREFIX_TERM=33,WILDCARD_TERM=34,HEX_NUMBER=35};

internal partial struct ValueType
{ 
			public string s; 
			public FieldLuceneASTNode fn;
			public ParenthesistLuceneASTNode pn;
			public PostfixModifiers pm;
			public LuceneASTNodeBase nb;
			public OperatorLuceneASTNode.Operator o;
			public RangeLuceneASTNode rn;
			public TermLuceneASTNode tn;
			public MethodLuceneASTNode mn;
			public List<TermLuceneASTNode> ltn;
			public LuceneASTNodeBase.PrefixOperator npo;
	   }
// Abstract base class for GPLEX scanners
[GeneratedCodeAttribute( "Gardens Point Parser Generator", "1.5.2")]
internal abstract class ScanBase : AbstractScanner<ValueType,LexLocation> {
  private LexLocation __yylloc = new LexLocation();
  public override LexLocation yylloc { get { return __yylloc; } set { __yylloc = value; } }
  protected virtual bool yywrap() { return true; }
}

// Utility class for encapsulating token information
[GeneratedCodeAttribute( "Gardens Point Parser Generator", "1.5.2")]
internal class ScanObj {
  public int token;
  public ValueType yylval;
  public LexLocation yylloc;
  public ScanObj( int t, ValueType val, LexLocation loc ) {
    this.token = t; this.yylval = val; this.yylloc = loc;
  }
}

[GeneratedCodeAttribute( "Gardens Point Parser Generator", "1.5.2")]
internal partial class LuceneQueryParser: ShiftReduceParser<ValueType, LexLocation>
{
  // Verbatim content from Indexing\LuceneQuery.Language.grammar.y - 11/7/2016 3:57:54 PM
	public LuceneASTNodeBase LuceneAST {get; set;}
  // End verbatim content from Indexing\LuceneQuery.Language.grammar.y - 11/7/2016 3:57:54 PM

#pragma warning disable 649
  private static Dictionary<int, string> aliases;
#pragma warning restore 649
  private static Rule[] rules = new Rule[63];
  private static State[] states = new State[89];
  private static string[] nonTerms = new string[] {
      "main", "prefix_operator", "methodName", "fieldname", "fuzzy_modifier", 
      "boost_modifier", "proximity_modifier", "operator", "term_exp", "term", 
      "postfix_modifier", "paren_exp", "node", "field_exp", "range_operator_exp", 
      "method_exp", "term_match_list", "$accept", };

  static LuceneQueryParser() {
    states[0] = new State(new int[]{4,11,24,65,19,61,8,57,9,58,25,24,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,23,85,21,88},new int[]{-1,1,-13,3,-14,13,-4,14,-12,67,-9,68,-2,69,-10,59,-16,87,-3,77});
    states[1] = new State(new int[]{3,2});
    states[2] = new State(-1);
    states[3] = new State(new int[]{3,4,5,8,6,9,7,10,4,11,24,65,19,61,8,57,9,58,25,24,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,23,85,21,88},new int[]{-8,5,-13,7,-14,13,-4,14,-12,67,-9,68,-2,69,-10,59,-16,87,-3,77});
    states[4] = new State(-2);
    states[5] = new State(new int[]{4,11,24,65,19,61,8,57,9,58,25,24,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,23,85,21,88},new int[]{-13,6,-14,13,-4,14,-12,67,-9,68,-2,69,-10,59,-16,87,-3,77});
    states[6] = new State(new int[]{5,8,6,9,7,10,4,11,24,65,19,61,8,57,9,58,25,24,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,23,85,21,88,3,-4,20,-4},new int[]{-8,5,-13,7,-14,13,-4,14,-12,67,-9,68,-2,69,-10,59,-16,87,-3,77});
    states[7] = new State(new int[]{5,8,6,9,7,10,4,11,24,65,19,61,8,57,9,58,25,24,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,23,85,21,88,3,-5,20,-5},new int[]{-8,5,-13,7,-14,13,-4,14,-12,67,-9,68,-2,69,-10,59,-16,87,-3,77});
    states[8] = new State(-58);
    states[9] = new State(-59);
    states[10] = new State(-60);
    states[11] = new State(new int[]{4,11,24,65,19,61,8,57,9,58,25,24,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,23,85,21,88},new int[]{-13,12,-14,13,-4,14,-12,67,-9,68,-2,69,-10,59,-16,87,-3,77});
    states[12] = new State(new int[]{5,8,6,9,7,10,4,11,24,65,19,61,8,57,9,58,25,24,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,23,85,21,88,3,-3,20,-3},new int[]{-8,5,-13,7,-14,13,-4,14,-12,67,-9,68,-2,69,-10,59,-16,87,-3,77});
    states[13] = new State(-6);
    states[14] = new State(new int[]{10,18,12,37,8,57,9,58,25,24,24,25,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,19,61},new int[]{-15,15,-9,16,-12,17,-2,43,-10,59});
    states[15] = new State(-16);
    states[16] = new State(-17);
    states[17] = new State(-18);
    states[18] = new State(new int[]{25,24,24,25,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36},new int[]{-10,19});
    states[19] = new State(new int[]{17,20});
    states[20] = new State(new int[]{25,24,24,25,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36},new int[]{-10,21});
    states[21] = new State(new int[]{11,22,13,23});
    states[22] = new State(-54);
    states[23] = new State(-56);
    states[24] = new State(-31);
    states[25] = new State(-32);
    states[26] = new State(-33);
    states[27] = new State(-34);
    states[28] = new State(-35);
    states[29] = new State(-36);
    states[30] = new State(-37);
    states[31] = new State(-38);
    states[32] = new State(-39);
    states[33] = new State(-40);
    states[34] = new State(-41);
    states[35] = new State(-42);
    states[36] = new State(-43);
    states[37] = new State(new int[]{25,24,24,25,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36},new int[]{-10,38});
    states[38] = new State(new int[]{17,39});
    states[39] = new State(new int[]{25,24,24,25,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36},new int[]{-10,40});
    states[40] = new State(new int[]{11,41,13,42});
    states[41] = new State(-55);
    states[42] = new State(-57);
    states[43] = new State(new int[]{25,24,24,25,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36},new int[]{-10,44});
    states[44] = new State(new int[]{14,51,15,48,3,-29,5,-29,6,-29,7,-29,4,-29,24,-29,19,-29,8,-29,9,-29,25,-29,28,-29,27,-29,35,-29,30,-29,29,-29,22,-29,31,-29,32,-29,26,-29,34,-29,33,-29,23,-29,21,-29,20,-29},new int[]{-11,45,-7,46,-5,54,-6,56});
    states[45] = new State(-27);
    states[46] = new State(new int[]{15,48,3,-48,5,-48,6,-48,7,-48,4,-48,24,-48,19,-48,8,-48,9,-48,25,-48,28,-48,27,-48,35,-48,30,-48,29,-48,22,-48,31,-48,32,-48,26,-48,34,-48,33,-48,23,-48,21,-48,20,-48},new int[]{-6,47});
    states[47] = new State(-44);
    states[48] = new State(new int[]{28,49,27,50});
    states[49] = new State(-50);
    states[50] = new State(-51);
    states[51] = new State(new int[]{28,52,27,53,15,-53,3,-53,5,-53,6,-53,7,-53,4,-53,24,-53,19,-53,8,-53,9,-53,25,-53,35,-53,30,-53,29,-53,22,-53,31,-53,32,-53,26,-53,34,-53,33,-53,23,-53,21,-53,20,-53});
    states[52] = new State(-49);
    states[53] = new State(-52);
    states[54] = new State(new int[]{15,48,3,-47,5,-47,6,-47,7,-47,4,-47,24,-47,19,-47,8,-47,9,-47,25,-47,28,-47,27,-47,35,-47,30,-47,29,-47,22,-47,31,-47,32,-47,26,-47,34,-47,33,-47,23,-47,21,-47,20,-47},new int[]{-6,55});
    states[55] = new State(-45);
    states[56] = new State(-46);
    states[57] = new State(-61);
    states[58] = new State(-62);
    states[59] = new State(new int[]{14,51,15,48,3,-30,5,-30,6,-30,7,-30,4,-30,24,-30,19,-30,8,-30,9,-30,25,-30,28,-30,27,-30,35,-30,30,-30,29,-30,22,-30,31,-30,32,-30,26,-30,34,-30,33,-30,23,-30,21,-30,20,-30},new int[]{-11,60,-7,46,-5,54,-6,56});
    states[60] = new State(-28);
    states[61] = new State(new int[]{4,11,24,65,19,61,8,57,9,58,25,24,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,23,85,21,88},new int[]{-13,62,-14,13,-4,14,-12,67,-9,68,-2,69,-10,59,-16,87,-3,77});
    states[62] = new State(new int[]{20,63,5,8,6,9,7,10,4,11,24,65,19,61,8,57,9,58,25,24,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,23,85,21,88},new int[]{-8,5,-13,7,-14,13,-4,14,-12,67,-9,68,-2,69,-10,59,-16,87,-3,77});
    states[63] = new State(new int[]{15,48,3,-23,5,-23,6,-23,7,-23,4,-23,24,-23,19,-23,8,-23,9,-23,25,-23,28,-23,27,-23,35,-23,30,-23,29,-23,22,-23,31,-23,32,-23,26,-23,34,-23,33,-23,23,-23,21,-23,20,-23},new int[]{-6,64});
    states[64] = new State(-24);
    states[65] = new State(new int[]{18,66,14,-32,15,-32,3,-32,5,-32,6,-32,7,-32,4,-32,24,-32,19,-32,8,-32,9,-32,25,-32,28,-32,27,-32,35,-32,30,-32,29,-32,22,-32,31,-32,32,-32,26,-32,34,-32,33,-32,23,-32,21,-32,20,-32});
    states[66] = new State(-26);
    states[67] = new State(-7);
    states[68] = new State(-8);
    states[69] = new State(new int[]{21,76,25,24,24,65,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,19,61,8,57,9,58,23,85},new int[]{-10,70,-14,72,-12,73,-9,74,-16,75,-4,14,-2,43,-3,77});
    states[70] = new State(new int[]{14,51,15,48,3,-29,5,-29,6,-29,7,-29,4,-29,24,-29,19,-29,8,-29,9,-29,25,-29,28,-29,27,-29,35,-29,30,-29,29,-29,22,-29,31,-29,32,-29,26,-29,34,-29,33,-29,23,-29,21,-29,20,-29},new int[]{-11,71,-7,46,-5,54,-6,56});
    states[71] = new State(-27);
    states[72] = new State(-10);
    states[73] = new State(-11);
    states[74] = new State(-12);
    states[75] = new State(-13);
    states[76] = new State(-14);
    states[77] = new State(new int[]{19,78});
    states[78] = new State(new int[]{8,57,9,58,25,24,24,25,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36},new int[]{-17,79,-9,81,-2,43,-10,59});
    states[79] = new State(new int[]{20,80});
    states[80] = new State(-19);
    states[81] = new State(new int[]{20,82,8,57,9,58,25,24,24,25,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36},new int[]{-9,83,-17,84,-2,43,-10,59});
    states[82] = new State(-20);
    states[83] = new State(new int[]{8,57,9,58,25,24,24,25,28,26,27,27,35,28,30,29,29,30,22,31,31,32,32,33,26,34,34,35,33,36,20,-21},new int[]{-9,83,-17,84,-2,43,-10,59});
    states[84] = new State(-22);
    states[85] = new State(new int[]{18,86});
    states[86] = new State(-25);
    states[87] = new State(-9);
    states[88] = new State(-15);

    for (int sNo = 0; sNo < states.Length; sNo++) states[sNo].number = sNo;

    rules[1] = new Rule(-18, new int[]{-1,3});
    rules[2] = new Rule(-1, new int[]{-13,3});
    rules[3] = new Rule(-13, new int[]{4,-13});
    rules[4] = new Rule(-13, new int[]{-13,-8,-13});
    rules[5] = new Rule(-13, new int[]{-13,-13});
    rules[6] = new Rule(-13, new int[]{-14});
    rules[7] = new Rule(-13, new int[]{-12});
    rules[8] = new Rule(-13, new int[]{-9});
    rules[9] = new Rule(-13, new int[]{-16});
    rules[10] = new Rule(-13, new int[]{-2,-14});
    rules[11] = new Rule(-13, new int[]{-2,-12});
    rules[12] = new Rule(-13, new int[]{-2,-9});
    rules[13] = new Rule(-13, new int[]{-2,-16});
    rules[14] = new Rule(-13, new int[]{-2,21});
    rules[15] = new Rule(-13, new int[]{21});
    rules[16] = new Rule(-14, new int[]{-4,-15});
    rules[17] = new Rule(-14, new int[]{-4,-9});
    rules[18] = new Rule(-14, new int[]{-4,-12});
    rules[19] = new Rule(-16, new int[]{-3,19,-17,20});
    rules[20] = new Rule(-16, new int[]{-3,19,-9,20});
    rules[21] = new Rule(-17, new int[]{-9,-9});
    rules[22] = new Rule(-17, new int[]{-9,-17});
    rules[23] = new Rule(-12, new int[]{19,-13,20});
    rules[24] = new Rule(-12, new int[]{19,-13,20,-6});
    rules[25] = new Rule(-3, new int[]{23,18});
    rules[26] = new Rule(-4, new int[]{24,18});
    rules[27] = new Rule(-9, new int[]{-2,-10,-11});
    rules[28] = new Rule(-9, new int[]{-10,-11});
    rules[29] = new Rule(-9, new int[]{-2,-10});
    rules[30] = new Rule(-9, new int[]{-10});
    rules[31] = new Rule(-10, new int[]{25});
    rules[32] = new Rule(-10, new int[]{24});
    rules[33] = new Rule(-10, new int[]{28});
    rules[34] = new Rule(-10, new int[]{27});
    rules[35] = new Rule(-10, new int[]{35});
    rules[36] = new Rule(-10, new int[]{30});
    rules[37] = new Rule(-10, new int[]{29});
    rules[38] = new Rule(-10, new int[]{22});
    rules[39] = new Rule(-10, new int[]{31});
    rules[40] = new Rule(-10, new int[]{32});
    rules[41] = new Rule(-10, new int[]{26});
    rules[42] = new Rule(-10, new int[]{34});
    rules[43] = new Rule(-10, new int[]{33});
    rules[44] = new Rule(-11, new int[]{-7,-6});
    rules[45] = new Rule(-11, new int[]{-5,-6});
    rules[46] = new Rule(-11, new int[]{-6});
    rules[47] = new Rule(-11, new int[]{-5});
    rules[48] = new Rule(-11, new int[]{-7});
    rules[49] = new Rule(-7, new int[]{14,28});
    rules[50] = new Rule(-6, new int[]{15,28});
    rules[51] = new Rule(-6, new int[]{15,27});
    rules[52] = new Rule(-5, new int[]{14,27});
    rules[53] = new Rule(-5, new int[]{14});
    rules[54] = new Rule(-15, new int[]{10,-10,17,-10,11});
    rules[55] = new Rule(-15, new int[]{12,-10,17,-10,11});
    rules[56] = new Rule(-15, new int[]{10,-10,17,-10,13});
    rules[57] = new Rule(-15, new int[]{12,-10,17,-10,13});
    rules[58] = new Rule(-8, new int[]{5});
    rules[59] = new Rule(-8, new int[]{6});
    rules[60] = new Rule(-8, new int[]{7});
    rules[61] = new Rule(-2, new int[]{8});
    rules[62] = new Rule(-2, new int[]{9});
  }

  protected override void Initialize() {
    this.InitSpecialTokens((int)Token.error, (int)Token.EOF);
    this.InitStates(states);
    this.InitRules(rules);
    this.InitNonTerminals(nonTerms);
  }

  protected override void DoAction(int action)
  {
#pragma warning disable 162, 1522
    switch (action)
    {
      case 2: // main -> node, EOF
{
	//Console.WriteLine("Found rule main -> node EOF");
	CurrentSemanticValue.nb = ValueStack[ValueStack.Depth-2].nb;
	LuceneAST = CurrentSemanticValue.nb;
	}
        break;
      case 3: // node -> NOT, node
{
		//Console.WriteLine("Found rule node -> NOT node");
		CurrentSemanticValue.nb = new OperatorLuceneASTNode(ValueStack[ValueStack.Depth-1].nb,null,OperatorLuceneASTNode.Operator.NOT);
	}
        break;
      case 4: // node -> node, operator, node
{
		//Console.WriteLine("Found rule node -> node operator node");
		var res =  new OperatorLuceneASTNode(ValueStack[ValueStack.Depth-3].nb,ValueStack[ValueStack.Depth-1].nb,ValueStack[ValueStack.Depth-2].o);
		CurrentSemanticValue.nb = res;
	}
        break;
      case 5: // node -> node, node
{
		//Console.WriteLine("Found rule node -> node node");
		CurrentSemanticValue.nb = new OperatorLuceneASTNode(ValueStack[ValueStack.Depth-2].nb,ValueStack[ValueStack.Depth-1].nb,OperatorLuceneASTNode.Operator.Implicit);
	}
        break;
      case 6: // node -> field_exp
{
		//Console.WriteLine("Found rule node -> field_exp");
		CurrentSemanticValue.nb =ValueStack[ValueStack.Depth-1].fn;
	}
        break;
      case 7: // node -> paren_exp
{
		//Console.WriteLine("Found rule node -> paren_exp");
		CurrentSemanticValue.nb =ValueStack[ValueStack.Depth-1].pn;
	}
        break;
      case 8: // node -> term_exp
{
	//Console.WriteLine("Found rule node -> term_exp");
		CurrentSemanticValue.nb = ValueStack[ValueStack.Depth-1].tn;
	}
        break;
      case 9: // node -> method_exp
{
		//Console.WriteLine("Found rule node -> method_exp");
		CurrentSemanticValue.nb = ValueStack[ValueStack.Depth-1].mn;
	}
        break;
      case 10: // node -> prefix_operator, field_exp
{
		//Console.WriteLine("Found rule node -> prefix_operator field_exp");
		CurrentSemanticValue.nb =ValueStack[ValueStack.Depth-1].fn;
		CurrentSemanticValue.nb.Prefix = ValueStack[ValueStack.Depth-2].npo;
	}
        break;
      case 11: // node -> prefix_operator, paren_exp
{
		//Console.WriteLine("Found rule node -> prefix_operator paren_exp");
		CurrentSemanticValue.nb =ValueStack[ValueStack.Depth-1].pn;
		CurrentSemanticValue.nb.Prefix = ValueStack[ValueStack.Depth-2].npo;
	}
        break;
      case 12: // node -> prefix_operator, term_exp
{
	//Console.WriteLine("Found rule node -> prefix_operator term_exp");
		CurrentSemanticValue.nb = ValueStack[ValueStack.Depth-1].tn;
		CurrentSemanticValue.nb.Prefix = ValueStack[ValueStack.Depth-2].npo;
	}
        break;
      case 13: // node -> prefix_operator, method_exp
{
		//Console.WriteLine("Found rule node -> prefix_operator method_exp");
		CurrentSemanticValue.nb = ValueStack[ValueStack.Depth-1].mn;
		CurrentSemanticValue.nb.Prefix = ValueStack[ValueStack.Depth-2].npo;
	}
        break;
      case 14: // node -> prefix_operator, ALL_DOC
{
		//Console.WriteLine("Found rule node -> prefix_operator ALL_DOC");
		CurrentSemanticValue.nb = new AllDocumentsLuceneASTNode();
		CurrentSemanticValue.nb.Prefix = ValueStack[ValueStack.Depth-2].npo;
	}
        break;
      case 15: // node -> ALL_DOC
{
		CurrentSemanticValue.nb = new AllDocumentsLuceneASTNode();
	}
        break;
      case 16: // field_exp -> fieldname, range_operator_exp
{
		//Console.WriteLine("Found rule field_exp -> fieldname range_operator_exp");		
		CurrentSemanticValue.fn = new FieldLuceneASTNode(){FieldName = ValueStack[ValueStack.Depth-2].s, Node = ValueStack[ValueStack.Depth-1].rn};
		}
        break;
      case 17: // field_exp -> fieldname, term_exp
{
		//Console.WriteLine("Found rule field_exp -> fieldname term_exp");
		CurrentSemanticValue.fn = new FieldLuceneASTNode(){FieldName = ValueStack[ValueStack.Depth-2].s, Node = ValueStack[ValueStack.Depth-1].tn};
		}
        break;
      case 18: // field_exp -> fieldname, paren_exp
{
		//Console.WriteLine("Found rule field_exp -> fieldname paren_exp");
		CurrentSemanticValue.fn = new FieldLuceneASTNode(){FieldName = ValueStack[ValueStack.Depth-2].s, Node = ValueStack[ValueStack.Depth-1].pn};
	}
        break;
      case 19: // method_exp -> methodName, OPEN_PAREN, term_match_list, CLOSE_PAREN
{
		//Console.WriteLine("Found rule method_exp -> methodName OPEN_PAREN term_match_list CLOSE_PAREN");
		CurrentSemanticValue.mn = new MethodLuceneASTNode(ValueStack[ValueStack.Depth-4].s,ValueStack[ValueStack.Depth-2].ltn);
		InMethod = false;
}
        break;
      case 20: // method_exp -> methodName, OPEN_PAREN, term_exp, CLOSE_PAREN
{
		//Console.WriteLine("Found rule method_exp -> methodName OPEN_PAREN term_exp CLOSE_PAREN");
		CurrentSemanticValue.mn = new MethodLuceneASTNode(ValueStack[ValueStack.Depth-4].s,ValueStack[ValueStack.Depth-2].tn);
		InMethod = false;
}
        break;
      case 21: // term_match_list -> term_exp, term_exp
{
	//Console.WriteLine("Found rule term_match_list -> term_exp term_exp");
	CurrentSemanticValue.ltn = new List<TermLuceneASTNode>(){ValueStack[ValueStack.Depth-2].tn,ValueStack[ValueStack.Depth-1].tn};
}
        break;
      case 22: // term_match_list -> term_exp, term_match_list
{
	//Console.WriteLine("Found rule term_match_list -> term_exp term_match_list");
	ValueStack[ValueStack.Depth-1].ltn.Add(ValueStack[ValueStack.Depth-2].tn);
	CurrentSemanticValue.ltn = ValueStack[ValueStack.Depth-1].ltn;
}
        break;
      case 23: // paren_exp -> OPEN_PAREN, node, CLOSE_PAREN
{
		//Console.WriteLine("Found rule paren_exp -> OPEN_PAREN node CLOSE_PAREN");
		CurrentSemanticValue.pn = new ParenthesistLuceneASTNode();
		CurrentSemanticValue.pn.Node = ValueStack[ValueStack.Depth-2].nb;
		}
        break;
      case 24: // paren_exp -> OPEN_PAREN, node, CLOSE_PAREN, boost_modifier
{
		//Console.WriteLine("Found rule paren_exp -> OPEN_PAREN node CLOSE_PAREN boost_modifier");
		CurrentSemanticValue.pn = new ParenthesistLuceneASTNode();
		CurrentSemanticValue.pn.Node = ValueStack[ValueStack.Depth-3].nb;
		CurrentSemanticValue.pn.Boost = ValueStack[ValueStack.Depth-1].s;
		}
        break;
      case 25: // methodName -> METHOD, COLON
{
		//Console.WriteLine("Found rule methodName -> METHOD COLON");
		CurrentSemanticValue.s = ValueStack[ValueStack.Depth-2].s;
		InMethod = true;
}
        break;
      case 26: // fieldname -> UNQUOTED_TERM, COLON
{
		//Console.WriteLine("Found rule fieldname -> UNQUOTED_TERM COLON");
		CurrentSemanticValue.s = ValueStack[ValueStack.Depth-2].s;
	}
        break;
      case 27: // term_exp -> prefix_operator, term, postfix_modifier
{
		//Console.WriteLine("Found rule term_exp -> prefix_operator term postfix_modifier");
		CurrentSemanticValue.tn = ValueStack[ValueStack.Depth-2].tn;
		CurrentSemanticValue.tn.Prefix =ValueStack[ValueStack.Depth-3].npo;
		CurrentSemanticValue.tn.SetPostfixOperators(ValueStack[ValueStack.Depth-1].pm);
	}
        break;
      case 28: // term_exp -> term, postfix_modifier
{
		//Console.WriteLine("Found rule term_exp -> postfix_modifier");
		CurrentSemanticValue.tn = ValueStack[ValueStack.Depth-2].tn;
		CurrentSemanticValue.tn.SetPostfixOperators(ValueStack[ValueStack.Depth-1].pm);
	}
        break;
      case 29: // term_exp -> prefix_operator, term
{
		//Console.WriteLine("Found rule term_exp -> prefix_operator term");
		CurrentSemanticValue.tn = ValueStack[ValueStack.Depth-1].tn;
		CurrentSemanticValue.tn.Prefix = ValueStack[ValueStack.Depth-2].npo;
	}
        break;
      case 30: // term_exp -> term
{
		//Console.WriteLine("Found rule term_exp -> term");
		CurrentSemanticValue.tn = ValueStack[ValueStack.Depth-1].tn;
	}
        break;
      case 31: // term -> QUOTED_TERM
{
		//Console.WriteLine("Found rule term -> QUOTED_TERM");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s.Substring(1,ValueStack[ValueStack.Depth-1].s.Length-2), Type=TermLuceneASTNode.TermType.Quoted};
	}
        break;
      case 32: // term -> UNQUOTED_TERM
{
		//Console.WriteLine("Found rule term -> UNQUOTED_TERM");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s,Type=TermLuceneASTNode.TermType.UnQuoted};
		}
        break;
      case 33: // term -> INT_NUMBER
{
		//Console.WriteLine("Found rule term -> INT_NUMBER");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s, Type=TermLuceneASTNode.TermType.Int};
		}
        break;
      case 34: // term -> FLOAT_NUMBER
{
		//Console.WriteLine("Found rule term -> FLOAT_NUMBER");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s, Type=TermLuceneASTNode.TermType.Float};
	}
        break;
      case 35: // term -> HEX_NUMBER
{
		//Console.WriteLine("Found rule term -> HEX_NUMBER");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s, Type=TermLuceneASTNode.TermType.Hex};
	}
        break;
      case 36: // term -> LONG_NUMBER
{
		//Console.WriteLine("Found rule term -> INT_NUMBER");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s, Type=TermLuceneASTNode.TermType.Long};
		}
        break;
      case 37: // term -> DOUBLE_NUMBER
{
		//Console.WriteLine("Found rule term -> FLOAT_NUMBER");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s, Type=TermLuceneASTNode.TermType.Double};
	}
        break;
      case 38: // term -> UNANALIZED_TERM
{
		//Console.WriteLine("Found rule term -> UNANALIZED_TERM");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s, Type=TermLuceneASTNode.TermType.UnAnalyzed};
	}
        break;
      case 39: // term -> DATETIME
{
		//Console.WriteLine("Found rule term -> DATETIME");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s, Type=TermLuceneASTNode.TermType.DateTime};
	}
        break;
      case 40: // term -> NULL
{
		//Console.WriteLine("Found rule term -> NULL");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s, Type=TermLuceneASTNode.TermType.Null};
	}
        break;
      case 41: // term -> QUOTED_WILDCARD_TERM
{
		//Console.WriteLine("Found rule term -> QUOTED_WILDCARD_TERM");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s, Type=TermLuceneASTNode.TermType.QuotedWildcard};
	}
        break;
      case 42: // term -> WILDCARD_TERM
{
		//Console.WriteLine("Found rule term -> WILDCARD_TERM");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s, Type=TermLuceneASTNode.TermType.WildCardTerm};
	}
        break;
      case 43: // term -> PREFIX_TERM
{
		//Console.WriteLine("Found rule term -> PREFIX_TERM");
		CurrentSemanticValue.tn = new TermLuceneASTNode(){Term=ValueStack[ValueStack.Depth-1].s, Type=TermLuceneASTNode.TermType.PrefixTerm};
	}
        break;
      case 44: // postfix_modifier -> proximity_modifier, boost_modifier
{
		CurrentSemanticValue.pm = new PostfixModifiers(){Boost = ValueStack[ValueStack.Depth-1].s, Similerity = null, Proximity = ValueStack[ValueStack.Depth-2].s};
	}
        break;
      case 45: // postfix_modifier -> fuzzy_modifier, boost_modifier
{
		CurrentSemanticValue.pm = new PostfixModifiers(){Boost = ValueStack[ValueStack.Depth-1].s, Similerity = ValueStack[ValueStack.Depth-2].s, Proximity = null};
	}
        break;
      case 46: // postfix_modifier -> boost_modifier
{
		CurrentSemanticValue.pm = new PostfixModifiers(){Boost = ValueStack[ValueStack.Depth-1].s,Similerity = null, Proximity = null};
	}
        break;
      case 47: // postfix_modifier -> fuzzy_modifier
{
		CurrentSemanticValue.pm = new PostfixModifiers(){Boost = null, Similerity = ValueStack[ValueStack.Depth-1].s, Proximity = null};
	}
        break;
      case 48: // postfix_modifier -> proximity_modifier
{
		CurrentSemanticValue.pm = new PostfixModifiers(){Boost = null, Similerity = null, Proximity = ValueStack[ValueStack.Depth-1].s};
	}
        break;
      case 49: // proximity_modifier -> TILDA, INT_NUMBER
{
	//Console.WriteLine("Found rule proximity_modifier -> TILDA INT_NUMBER");
	CurrentSemanticValue.s = ValueStack[ValueStack.Depth-1].s;
	}
        break;
      case 50: // boost_modifier -> BOOST, INT_NUMBER
{
	//Console.WriteLine("Found rule boost_modifier -> BOOST INT_NUMBER");
	CurrentSemanticValue.s = ValueStack[ValueStack.Depth-1].s;
	}
        break;
      case 51: // boost_modifier -> BOOST, FLOAT_NUMBER
{
	//Console.WriteLine("Found rule boost_modifier -> BOOST FLOAT_NUMBER");
	CurrentSemanticValue.s = ValueStack[ValueStack.Depth-1].s;
	}
        break;
      case 52: // fuzzy_modifier -> TILDA, FLOAT_NUMBER
{
	//Console.WriteLine("Found rule fuzzy_modifier ->  TILDA FLOAT_NUMBER");
	CurrentSemanticValue.s = ValueStack[ValueStack.Depth-1].s;
	}
        break;
      case 53: // fuzzy_modifier -> TILDA
{
		//Console.WriteLine("Found rule fuzzy_modifier ->  TILDA");
		CurrentSemanticValue.s = "0.5";
	}
        break;
      case 54: // range_operator_exp -> OPEN_CURLY_BRACKET, term, TO, term, CLOSE_CURLY_BRACKET
{
		//Console.WriteLine("Found rule range_operator_exp -> OPEN_CURLY_BRACKET term TO term CLOSE_CURLY_BRACKET");
		CurrentSemanticValue.rn = new RangeLuceneASTNode(){RangeMin = ValueStack[ValueStack.Depth-4].tn, RangeMax = ValueStack[ValueStack.Depth-2].tn, InclusiveMin = false, InclusiveMax = false};
		}
        break;
      case 55: // range_operator_exp -> OPEN_SQUARE_BRACKET, term, TO, term, CLOSE_CURLY_BRACKET
{
		//Console.WriteLine("Found rule range_operator_exp -> OPEN_SQUARE_BRACKET term TO term CLOSE_CURLY_BRACKET");
		CurrentSemanticValue.rn = new RangeLuceneASTNode(){RangeMin = ValueStack[ValueStack.Depth-4].tn, RangeMax = ValueStack[ValueStack.Depth-2].tn, InclusiveMin = true, InclusiveMax = false};
		}
        break;
      case 56: // range_operator_exp -> OPEN_CURLY_BRACKET, term, TO, term, CLOSE_SQUARE_BRACKET
{
		//Console.WriteLine("Found rule range_operator_exp -> OPEN_CURLY_BRACKET term TO term CLOSE_SQUARE_BRACKET");
		CurrentSemanticValue.rn = new RangeLuceneASTNode(){RangeMin = ValueStack[ValueStack.Depth-4].tn, RangeMax = ValueStack[ValueStack.Depth-2].tn, InclusiveMin = false, InclusiveMax = true};
		}
        break;
      case 57: // range_operator_exp -> OPEN_SQUARE_BRACKET, term, TO, term, CLOSE_SQUARE_BRACKET
{
		//Console.WriteLine("Found rule range_operator_exp -> OPEN_SQUARE_BRACKET term TO term CLOSE_SQUARE_BRACKET");
		CurrentSemanticValue.rn = new RangeLuceneASTNode(){RangeMin = ValueStack[ValueStack.Depth-4].tn, RangeMax = ValueStack[ValueStack.Depth-2].tn, InclusiveMin = true, InclusiveMax = true};
		}
        break;
      case 58: // operator -> OR
{
		//Console.WriteLine("Found rule operator -> OR");
		CurrentSemanticValue.o = OperatorLuceneASTNode.Operator.OR;
		}
        break;
      case 59: // operator -> AND
{
		//Console.WriteLine("Found rule operator -> AND");
		CurrentSemanticValue.o = OperatorLuceneASTNode.Operator.AND;
		}
        break;
      case 60: // operator -> INTERSECT
{
		//Console.WriteLine("Found rule operator -> INTERSECT");
		CurrentSemanticValue.o = OperatorLuceneASTNode.Operator.INTERSECT;
	}
        break;
      case 61: // prefix_operator -> PLUS
{
		//Console.WriteLine("Found rule prefix_operator -> PLUS");
		CurrentSemanticValue.npo = LuceneASTNodeBase.PrefixOperator.Plus;
		}
        break;
      case 62: // prefix_operator -> MINUS
{
		//Console.WriteLine("Found rule prefix_operator -> MINUS");
		CurrentSemanticValue.npo = LuceneASTNodeBase.PrefixOperator.Minus;
		}
        break;
    }
#pragma warning restore 162, 1522
  }

  protected override string TerminalToString(int terminal)
  {
    if (aliases != null && aliases.ContainsKey(terminal))
        return aliases[terminal];
    else if (((Token)terminal).ToString() != terminal.ToString(CultureInfo.InvariantCulture))
        return ((Token)terminal).ToString();
    else
        return CharToString((char)terminal);
  }

}
}
