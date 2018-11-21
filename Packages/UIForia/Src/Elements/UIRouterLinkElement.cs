using System;
using System.Collections.Generic;
using UIForia.Elements;
using UIForia.Rendering;
using UIForia.Util;
using UnityEngine;

namespace UIForia {

    [TemplateTagName("RouterLink")]
    public class UIRouterLinkElement : UIContainerElement {

        public string path;
        
        [OnMouseDown]
        public void GoToPath() {
            List<ElementAttribute> attrs = GetAttributes();
            for (int i = 0; i < attrs.Count; i++) {
                int index = attrs[i].name.IndexOf("parameters.", StringComparison.Ordinal);
                if (index == 0) {
                    //  Expression<string> exp = new ExpressionCompiler(null).Compile<string>(attrs[i].value);
                }
            }
            
            view.Application.Router.GoTo(path);
            
            ListPool<ElementAttribute>.Release(ref attrs);
        }
        
    }
    
    [TemplateTagName("RouterLinkBack")]
    public class UIRouterLinkBackElement : UIContainerElement {

        public override void OnUpdate() {
            bool canGoBack = view.Application.Router.CanGoBack;
            if (canGoBack) {
                style.ExitState(StyleState.Inactive);
            }
            else {
                style.EnterState(StyleState.Inactive);
            }
        }
        
        [OnMouseDown]
        public void GoBack() {
            view.Application.Router.GoBack();
        }
        
    }

}