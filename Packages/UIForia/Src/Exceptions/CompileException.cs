using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UIForia.Parsing.Expression.AstNodes;
using UIForia.Parsing.Style.AstNodes;
using Expression = UIForia.Expressions.Expression;

namespace UIForia.Exceptions {

    public class CompileException : Exception {

        private string fileName = "";
        public string expression = "";

        public CompileException(string message = null) : base(message) { }


        public CompileException(StyleASTNode node, string message = null) :
            base($"Compile error for style token at line {node.line}, column {node.column}, node type '{node.type}'\n{message}") { }

        public CompileException(string fileName, StyleASTNode node, string message = null) :
            base($"Compile error for style token at line {node.line}, column {node.column}, node type '{node.type}'\n{message}") {
            this.fileName = fileName;
        }

        public void SetFileName(string name) {
            this.fileName = "Error in file " + name + ": ";
        }

        public override string Message {
            get {
                string retn = fileName + "\n" + base.Message;

                if (!string.IsNullOrEmpty(expression)) {
                    retn += "\nExpression was: " + expression;
                }

                return retn;
            }
        }

        public void SetExpression(string input) {
            expression = input;
        }

        public static CompileException RHSRootIdentifierMissing(string identifierName) {
            return new CompileException($"Unable to compile right hand side expression beginning with {identifierName} because there was no root variable.");
        }

        public static CompileException NoStatementsRootBlock() {
            return new CompileException($"Cannot compile the lambda because there are not statements emitted in the main block of the function");
        }

        public static CompileException InvalidActionArgumentCount(List<ParameterExpression> parameters, Type[] genericArguments) {
            return new CompileException($"Cannot compile the action because the declared parameter count {parameters.Count} is not the same as the required signatures parameter count {genericArguments.Length}");
        }

        public static CompileException MissingBinaryOperator(OperatorType opType, Type a, Type b) {
            return new CompileException($"Missing operator: the binary operator {opType} is not defined for the types {a} and {b}");
        }

        public static CompileException UnresolvedIdentifier(string identifierNodeName) {
            return new CompileException($"Unable to resolve the variable or parameter {identifierNodeName}");
        }

        public static CompileException InvalidTargetType(Type expected, Type actual) {
            return new CompileException($"Expected expression to be compatible with type {expected} but got {actual} which was not convertable");
        }

        public static CompileException UnresolvedType(TypeLookup typeLookup) {
            return new CompileException($"Unable to resolve type {typeLookup}, are you missing a namespace?");
        }

    }

    public static class CompileExceptions {

        public static CompileException TooManyArgumentsException(string methodName, int argumentCount) {
            return new CompileException($"Expressions only support functions with up to 4 arguments. {methodName} is supplying {argumentCount} ");
        }

        public static CompileException MethodNotFound(Type definingType, string identifier, List<Expression> arguments = null) {
            return new CompileException($"Type {definingType} does not define a method '{identifier}' with matching signature");
        }

        public static Exception FieldOrPropertyNotFound(Type definingType, string propertyName) {
            return new CompileException($"Type {definingType} does not define a property or field with the name {propertyName}");
        }

    }

}