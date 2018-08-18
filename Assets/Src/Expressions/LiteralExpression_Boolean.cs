using System;

namespace Src {

    public class LiteralExpression_Boolean : Expression<bool> {

        // object type so we only box once
        public readonly bool value;
        private readonly object boxedValue;

        public LiteralExpression_Boolean(bool value) {
            this.value = value;
            this.boxedValue = value;
        }

        public override Type YieldedType => typeof(bool);
        
        public override bool EvaluateTyped(ExpressionContext context) {
            return value;
        }


        public override object Evaluate(ExpressionContext context) {
            return boxedValue;
        }

    }

}