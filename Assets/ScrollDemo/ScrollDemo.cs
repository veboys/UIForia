using UIForia.Attributes;
using UIForia.Elements;
using UIForia.Util;

namespace Demo {

    [Template("ScrollDemo/ScrollDemo.xml")]
    public class ScrollDemo : UIElement {

        public RepeatableList<int> list;

        public override void OnCreate() {
            list = new RepeatableList<int>();
            for (int i = 0; i < 100; i++) {
                list.Add(i);
            }
        }

    }

}