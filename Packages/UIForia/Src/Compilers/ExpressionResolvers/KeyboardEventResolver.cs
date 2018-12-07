using System;
using UIForia.Input;
using UIForia.Parsing;

namespace UIForia.Compilers {

    public class KeyboardEventResolver : ExpressionAliasResolver {

        public KeyboardEventResolver(string aliasName) : base(aliasName) { }

        private static readonly KeyboardEventAliasExpression s_Expression = new KeyboardEventAliasExpression();

//        public override Expression CompileAsValueExpression(ASTNode nodeOld, Func<Type, ASTNode, Expression>visit) {
//            return s_Expression;
//        }

        public class KeyboardEventAliasExpression : Expression<KeyboardInputEvent> {

            public override Type YieldedType => typeof(MouseInputEvent);


            public override KeyboardInputEvent Evaluate(ExpressionContext context) {
                UIElement element = (UIElement) context.currentObject;
                return element.view.Application.InputSystem.CurrentKeyboardEvent;
            }

            public override bool IsConstant() {
                return false;
            }

        }

    }

}