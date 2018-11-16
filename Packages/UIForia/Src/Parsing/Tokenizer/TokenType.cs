using System;

namespace UIForia {

    [Flags]
    public enum TokenType {

        ExpressionOpen = 1 << 0,
        ExpressionClose = 1 << 1,

        // operators
        Plus = 1 << 2,
        Minus = 1 << 3,
        Times = 1 << 4,
        Divide = 1 << 5,
        Mod = 1 << 6,

        // accessors
        Dot = 1 << 7,
        ArrayAccessOpen = 1 << 8,
        ArrayAccessClose = 1 << 9,
        FilterDelimiter = 1 << 10,
        ParenOpen = 1 << 26,
        ParenClose = 1 << 27,

        // identifiers
        Identifier = 1 << 11,
        SpecialIdentifier = 1 << 12,
        At = 1 << 30, 

        // constants
        String = 1 << 13,
        Boolean = 1 << 14,
        Number = 1 << 15,

        // booleans
        And = 1 << 16,
        Or = 1 << 17,
        Not = 1 << 18,

        // Comparators
        Equals = 1 << 19,
        NotEquals = 1 << 20,
        GreaterThan = 1 << 21,
        LessThan = 1 << 22,
        GreaterThanEqualTo = 1 << 23,
        LessThanEqualTo = 1 << 24,
        QuestionMark = 1 << 25,
        Colon = 1 << 28,
        Comma = 1 << 29,
        
        ArithmeticOperator = Plus | Minus | Times | Divide | Mod,
        Literal = String | Number | Boolean,
        Comparator = Equals | NotEquals | GreaterThan | GreaterThanEqualTo | LessThan | LessThanEqualTo,
        BooleanTest = Not | Or | And,
        AnyIdentifier = Identifier | SpecialIdentifier,
        UnaryOperator = Plus | Minus | Not,

        Operator = ArithmeticOperator | QuestionMark | Colon | Comparator | BooleanTest,

        UnaryRequiresCheck = Comma | Colon | QuestionMark | BooleanTest | ArithmeticOperator | Comparator | ParenOpen | ArrayAccessOpen

    }

}