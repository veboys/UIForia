using UIForia.Elements;
using UIForia.Util;

namespace UIForia.Systems {

    public class ImageLayoutBox : LayoutBox {

        protected override float ComputeContentWidth() {
            UIImageElement imageElement = (UIImageElement) element;
            if (imageElement.texture != null) {
                return imageElement.texture.width;
            }

            return 0; //imageElement.Width;
        }

        protected override float ComputeContentHeight() {
            UIImageElement imageElement = (UIImageElement) element;
            if (imageElement.texture != null) {
                return imageElement.texture.height;
            }

            return 0; //imageElement.Height;
        }

        public override void OnChildrenChanged() { }

        public override void RunLayoutHorizontal(int frameId) { }

        public override void RunLayoutVertical(int frameId) { }

    }

}