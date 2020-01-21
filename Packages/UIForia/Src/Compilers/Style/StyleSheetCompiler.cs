using System;
using System.Linq;
using UIForia.Animation;
using UIForia.Exceptions;
using UIForia.Parsing.Style;
using UIForia.Parsing.Style.AstNodes;
using UIForia.Rendering;
using UIForia.Sound;
using UIForia.Util;

namespace UIForia.Compilers.Style {

    public class StyleSheetCompiler {

        private StyleCompileContext context;
        private readonly StyleSheetImporter styleSheetImporter;

        private LightList<UIStyleGroup> scratchGroupList;

        private static readonly UIStyle s_ScratchStyle = new UIStyle();

        public StyleSheetCompiler(StyleSheetImporter styleSheetImporter) {
            this.styleSheetImporter = styleSheetImporter;
            this.scratchGroupList = new LightList<UIStyleGroup>(32);
        }

        public StyleSheet Compile(string filePath, string contents) {
            return Compile(filePath, StyleParser.Parse(contents));
        }

        // todo -- deprecate, use other method
        public StyleSheet Compile(string filePath, LightList<StyleASTNode> rootNodes) {
            try {
                context = new StyleSheetConstantImporter(styleSheetImporter).CreateContext(rootNodes);

                //       context = new StyleCompileContext(); // todo resolve constants. should be done a per file level, should store all used constants without needing to later reference other files
                // StyleCompileContext.Create(styleSheetImporter) //new StyleSheetConstantImporter(styleSheetImporter).CreateContext(rootNodes);
            }
            catch (CompileException e) {
                e.SetFileName(filePath);
                throw;
            }

            context.fileName = filePath;

            // todo add imported style groups

            rootNodes.Sort((node1, node2) => {
                int left = (int) node1.type;
                int right = (int) node2.type;
                return left - right;
            });

            int containerCount = 0;
            int animationCount = 0;
            int soundCount = 0;

            for (int index = 0; index < rootNodes.Count; index++) {
                switch (rootNodes[index]) {
                    case StyleRootNode _:
                        containerCount++;
                        break;
                    case AnimationRootNode _:
                    case SpriteSheetNode _:
                        animationCount++;
                        break;
                    case SoundRootNode _:
                        soundCount++;
                        break;
                }
            }

            StyleSheet styleSheet = new StyleSheet(
                styleSheetImporter.ImportedStyleSheetCount,
                context.constants?.ToArray(),
                containerCount > 0
                    ? new UIStyleGroupContainer[containerCount]
                    : ArrayPool<UIStyleGroupContainer>.Empty,
                animationCount > 0
                    ? new AnimationData[animationCount]
                    : ArrayPool<AnimationData>.Empty,
                soundCount > 0
                    ? new UISoundData[soundCount]
                    : ArrayPool<UISoundData>.Empty
            );

            int containerIndex = 0;
            int animationIndex = 0;
            int soundIndex = 0;

            for (int index = 0; index < rootNodes.Count; index++) {
                switch (rootNodes[index]) {
                    // we sorted the root nodes so all animations run first
                    case SpriteSheetNode spriteSheetNode:
                        styleSheet.animations[animationIndex] = CompileSpriteSheetAnimation(spriteSheetNode, styleSheet.animations, styleSheet.sounds);
                        animationIndex++;
                        break;
                    case AnimationRootNode animNode:
                        styleSheet.animations[animationIndex] = CompileAnimation(animNode, styleSheet.animations, styleSheet.sounds);
                        animationIndex++;
                        break;
                    case SoundRootNode soundRootNode:
                        styleSheet.sounds[soundIndex] = CompileSound(soundRootNode);
                        soundIndex++;
                        break;
                    case StyleRootNode styleRoot:
                        styleSheet.styleGroupContainers[containerIndex] = CompileStyleGroup(styleRoot, styleSheet.animations, styleSheet.sounds);
                        containerIndex++;
                        break;
                }
            }

            context.Release();
            return styleSheet;
        }

        private AnimationData CompileSpriteSheetAnimation(SpriteSheetNode node, AnimationData[] styleSheetAnimations, UISoundData[] uiSoundData) {
            AnimationData data = new AnimationData();
            data.name = node.identifier;
            data.fileName = context.fileName;
            data.animationType = AnimationType.SpriteSheet;
            data.options = CompileSpriteSheetOptions(node);
            return data;
        }

        private AnimationData CompileAnimation(AnimationRootNode animNode, AnimationData[] styleSheetAnimations, UISoundData[] uiSoundData) {
            AnimationData data = new AnimationData();
            data.name = animNode.animName;
            data.fileName = context.fileName;
            data.animationType = AnimationType.KeyFrame;
            data.frames = CompileKeyFrames(animNode, styleSheetAnimations, uiSoundData);
            data.options = CompileAnimationOptions(animNode);
            return data;
        }

        private UISoundData CompileSound(SoundRootNode soundRootNode) {
            UISoundData data = CompileSoundProperties(soundRootNode);
            data.name = soundRootNode.identifier;
            data.styleSheetFileName = context.fileName;
            return data;
        }

        private AnimationKeyFrame[] CompileKeyFrames(AnimationRootNode animNode, AnimationData[] styleSheetAnimations, UISoundData[] uiSoundData) {
            if (animNode.keyframeNodes == null) {
                // todo throw error or log warning?
                return new AnimationKeyFrame[0];
            }

            int keyframeCount = 0;
            for (int i = 0; i < animNode.keyframeNodes.Count; i++) {
                keyframeCount += animNode.keyframeNodes[i].keyframes.Count;
            }

            AnimationKeyFrame[] frames = new AnimationKeyFrame[keyframeCount];
            int nextKeyframeIndex = 0;
            for (int i = 0; i < animNode.keyframeNodes.Count; i++) {
                KeyFrameNode keyFrameNode = animNode.keyframeNodes[i];

                for (int j = 0; j < keyFrameNode.children.Count; j++) {
                    StyleASTNode keyFrameProperty = keyFrameNode.children[j];
                    if (keyFrameProperty is PropertyNode propertyNode) {
                        StylePropertyMappers.MapProperty(s_ScratchStyle, propertyNode, context);
                    }
                    else if (keyFrameProperty is RunNode runNode) {
                        //
                        // if (runNode.command is AnimationCommandNode animationCommandNode) {
                        //     MapAnimationRunCommand(styleSheetAnimations, animationCommandNode);
                        // } else if (runNode.command is SoundCommandNode soundCommandNode) {
                        //     MapSoundRunCommand(uiSoundData, soundCommandNode);
                        // }
                        // break;
                    }
                }

                int count = s_ScratchStyle.PropertyCount;
                StructList<StyleKeyFrameValue> keyValues = new StructList<StyleKeyFrameValue>(count);

                for (int j = 0; j < count; j++) {
                    keyValues[j] = new StyleKeyFrameValue(s_ScratchStyle[j]);
                }

                keyValues.size = count;

                for (int keyframeIndex = 0; keyframeIndex < keyFrameNode.keyframes.Count; keyframeIndex++) {
                    float time = keyFrameNode.keyframes[keyframeIndex] / 100f;
                    frames[nextKeyframeIndex] = new AnimationKeyFrame(time);
                    frames[nextKeyframeIndex].properties = keyValues;
                    nextKeyframeIndex++;
                }

                s_ScratchStyle.PropertyCount = 0;
            }

            return frames;
        }

        private AnimationOptions CompileAnimationOptions(AnimationRootNode animNode) {
            AnimationOptions options = new AnimationOptions();

            if (animNode.optionNodes == null) {
                return options;
            }

            LightList<AnimationOptionNode> optionNodes = animNode.optionNodes;
            if (optionNodes == null) {
                return options;
            }

            for (int i = 0; i < optionNodes.Count; i++) {
                string optionName = optionNodes[i].optionName;
                StyleASTNode value = optionNodes[i].value;

                switch (optionName) {
                    case nameof(AnimationOptions.duration):
                        options.duration = StylePropertyMappers.MapUITimeMeasurement(value, context);
                        break;
                    case nameof(AnimationOptions.iterations):
                        options.iterations = (int) StylePropertyMappers.MapNumberOrInfinite(value, context);
                        break;
                    case nameof(AnimationOptions.loopTime):
                        options.loopTime = StylePropertyMappers.MapNumber(value, context);
                        break;
                    case nameof(AnimationOptions.delay):
                        options.delay = StylePropertyMappers.MapUITimeMeasurement(value, context);
                        break;
                    case nameof(AnimationOptions.direction):
                        options.direction = StylePropertyMappers.MapEnum<AnimationDirection>(value, context);
                        break;
                    case nameof(AnimationOptions.loopType):
                        options.loopType = StylePropertyMappers.MapEnum<AnimationLoopType>(value, context);
                        break;
                    case nameof(AnimationOptions.forwardStartDelay):
                        options.forwardStartDelay = (int) StylePropertyMappers.MapNumber(value, context);
                        break;
                    case nameof(AnimationOptions.reverseStartDelay):
                        options.reverseStartDelay = (int) StylePropertyMappers.MapNumber(value, context);
                        break;
                    case nameof(AnimationOptions.timingFunction):
                        options.timingFunction = StylePropertyMappers.MapEnum<EasingFunction>(value, context);
                        break;
                    default:
                        throw new CompileException(optionNodes[i], "Invalid option argument for animation");
                }
            }

            return options;
        }

        private AnimationOptions CompileSpriteSheetOptions(SpriteSheetNode node) {
            AnimationOptions options = new AnimationOptions();

            LightList<StyleASTNode> spriteSheetProperties = node.children;
            if (spriteSheetProperties == null) {
                return options;
            }

            for (int i = 0; i < spriteSheetProperties.Count; i++) {
                if (spriteSheetProperties[i] is PropertyNode property) {
                    string optionName = property.identifier;
                    StyleASTNode value = property.children[0];

                    switch (optionName) {
                        case nameof(AnimationOptions.iterations):
                            options.iterations = (int) StylePropertyMappers.MapNumberOrInfinite(value, context);
                            break;
                        case nameof(AnimationOptions.delay):
                            options.delay = StylePropertyMappers.MapUITimeMeasurement(value, context);
                            break;
                        case nameof(AnimationOptions.duration):
                            options.duration = StylePropertyMappers.MapUITimeMeasurement(value, context);
                            break;
                        case nameof(AnimationOptions.loopType):
                            options.loopType = StylePropertyMappers.MapEnum<AnimationLoopType>(value, context);
                            break;
                        case nameof(AnimationOptions.direction):
                            options.direction = StylePropertyMappers.MapEnum<AnimationDirection>(value, context);
                            break;
                        case nameof(AnimationOptions.forwardStartDelay):
                            options.forwardStartDelay = (int) StylePropertyMappers.MapNumber(value, context);
                            break;
                        case nameof(AnimationOptions.reverseStartDelay):
                            options.reverseStartDelay = (int) StylePropertyMappers.MapNumber(value, context);
                            break;
                        case nameof(AnimationOptions.fps):
                            options.fps = (int) StylePropertyMappers.MapNumber(value, context);
                            break;
                        case nameof(AnimationOptions.startFrame):
                            options.startFrame = (int) StylePropertyMappers.MapNumber(value, context);
                            break;
                        case nameof(AnimationOptions.endFrame):
                            options.endFrame = (int) StylePropertyMappers.MapNumber(value, context);
                            break;
                        case nameof(AnimationOptions.pathPrefix):
                            options.pathPrefix = StylePropertyMappers.MapString(value, context);
                            break;

                        default:
                            throw new CompileException(property, "Invalid option argument for animation");
                    }
                }
                else {
                    throw new CompileException(spriteSheetProperties[i], "Invalid option argument for animation");
                }
            }

            return options;
        }

        private UISoundData CompileSoundProperties(SoundRootNode soundRootNode) {
            UISoundData soundData = new UISoundData();
            // set defaults
            soundData.duration = new UITimeMeasurement(1, UITimeMeasurementUnit.Percentage);
            soundData.pitch = 1;
            soundData.iterations = 1;
            soundData.tempo = 1;

            for (int i = 0; i < soundRootNode.children.Count; i++) {
                StyleASTNode property = soundRootNode.children[i];
                if (property is SoundPropertyNode soundPropertyNode) {
                    StyleASTNode value = soundPropertyNode.value;
                    switch (soundPropertyNode.name) {
                        case nameof(UISoundData.asset):
                            soundData.asset = StylePropertyMappers.MapString(value, context);
                            break;
                        case nameof(UISoundData.duration):
                            soundData.duration = StylePropertyMappers.MapUITimeMeasurement(value, context);
                            break;
                        case nameof(UISoundData.iterations):
                            soundData.iterations = (int) StylePropertyMappers.MapNumberOrInfinite(value, context);
                            break;
                        case nameof(UISoundData.pitch):
                            soundData.pitch = StylePropertyMappers.MapNumber(value, context);
                            break;
                        case nameof(UISoundData.pitchRange):
                            soundData.pitchRange = StylePropertyMappers.MapFloatRange(value, context);
                            break;
                        case nameof(UISoundData.tempo):
                            soundData.tempo = StylePropertyMappers.MapNumber(value, context);
                            break;
                        case nameof(UISoundData.volume):
                            soundData.volume = StylePropertyMappers.MapNumber(value, context);
                            break;
                        case nameof(UISoundData.mixerGroup):
                            soundData.mixerGroup = StylePropertyMappers.MapString(value, context);
                            break;
                    }
                }
                else {
                    throw new CompileException(property, "Expected a sound property.");
                }
            }

            return soundData;
        }

        private UIStyleGroupContainer CompileStyleGroup(StyleRootNode styleRoot, AnimationData[] styleSheetAnimations, UISoundData[] uiSoundData) {
            UIStyleGroup defaultGroup = new UIStyleGroup();
            defaultGroup.normal = UIStyleRunCommand.CreateInstance();
            defaultGroup.name = styleRoot.identifier ?? styleRoot.tagName;
            StyleType styleType = styleRoot.tagName != null ? StyleType.Implicit : StyleType.Shared;

            scratchGroupList.size = 0;

            scratchGroupList.Add(defaultGroup);

            CompileStyleGroups(styleRoot, styleType, scratchGroupList, defaultGroup, styleSheetAnimations, uiSoundData);

            return new UIStyleGroupContainer(styleSheetImporter.NextStyleGroupId, defaultGroup.name, styleType, scratchGroupList.ToArray());
        }

        private void CompileStyleGroups(StyleNodeContainer root, StyleType styleType, LightList<UIStyleGroup> groups, UIStyleGroup targetGroup, AnimationData[] styleSheetAnimations, UISoundData[] uiSoundData) {
            for (int index = 0; index < root.children.Count; index++) {
                StyleASTNode node = root.children[index];
                switch (node) {
                    case PropertyNode propertyNode:
                        // add to normal ui style set
                        StylePropertyMappers.MapProperty(targetGroup.normal.style, propertyNode, context);
                        break;
                    case AttributeNodeContainer attribute:
                        if (root is AttributeNodeContainer) {
                            throw new CompileException(attribute, "You cannot nest attribute group definitions.");
                        }

                        UIStyleGroup attributeGroup = new UIStyleGroup();
                        attributeGroup.normal = UIStyleRunCommand.CreateInstance();
                        attributeGroup.name = root.identifier;
                        attributeGroup.rule = MapAttributeContainerToRule(attribute);
                        attributeGroup.styleType = styleType;
                        groups.Add(attributeGroup);
                        CompileStyleGroups(attribute, styleType, groups, attributeGroup, styleSheetAnimations, uiSoundData);

                        break;
                    case RunNode runNode:
                        UIStyleRunCommand cmd = new UIStyleRunCommand() {
                            style = targetGroup.normal.style,
                            runCommands = targetGroup.normal.runCommands ?? new LightList<IRunCommand>(4)
                        };

                        if (runNode.command is AnimationCommandNode animationCommandNode) {
                            MapAnimationCommand(styleSheetAnimations, cmd, animationCommandNode);
                        }
                        else if (runNode.command is SoundCommandNode soundCommandNode) {
                            MapSoundCommand(uiSoundData, cmd, soundCommandNode);
                        }

                        targetGroup.normal = cmd;
                        break;
                    case StyleStateContainer styleContainer:
                        if (styleContainer.identifier == "hover") {
                            UIStyleRunCommand uiStyleRunCommand = targetGroup.hover;
                            uiStyleRunCommand.style = uiStyleRunCommand.style ?? new UIStyle();
                            MapProperties(styleSheetAnimations, uiSoundData, ref uiStyleRunCommand, styleContainer.children);
                            targetGroup.hover = uiStyleRunCommand;
                        }
                        else if (styleContainer.identifier == "focus") {
                            UIStyleRunCommand uiStyleRunCommand = targetGroup.focused;
                            uiStyleRunCommand.style = uiStyleRunCommand.style ?? new UIStyle();
                            MapProperties(styleSheetAnimations, uiSoundData, ref uiStyleRunCommand, styleContainer.children);
                            targetGroup.focused = uiStyleRunCommand;
                        }
                        else if (styleContainer.identifier == "active") {
                            UIStyleRunCommand uiStyleRunCommand = targetGroup.active;
                            uiStyleRunCommand.style = uiStyleRunCommand.style ?? new UIStyle();
                            MapProperties(styleSheetAnimations, uiSoundData, ref uiStyleRunCommand, styleContainer.children);
                            targetGroup.active = uiStyleRunCommand;
                        }
                        else throw new CompileException(styleContainer, $"Unknown style state '{styleContainer.identifier}'. Please use [hover], [focus] or [active] instead.");

                        break;
                    default:
                        throw new CompileException(node, $"You cannot have a {node} at this level.");
                }
            }
        }

        private void MapAnimationCommand(AnimationData[] styleSheetAnimations, UIStyleRunCommand cmd, AnimationCommandNode animationCommandNode) {
            cmd.runCommands.Add(MapAnimationRunCommand(styleSheetAnimations, animationCommandNode));
        }

        private AnimationRunCommand MapAnimationRunCommand(AnimationData[] styleSheetAnimations, AnimationCommandNode animationCommandNode) {
            return new AnimationRunCommand(animationCommandNode.isExit, animationCommandNode.runAction) {
                animationData = FindAnimationData(styleSheetAnimations, animationCommandNode.animationName),
            };
        }

        private void MapSoundCommand(UISoundData[] soundData, UIStyleRunCommand cmd, SoundCommandNode soundCommandNode) {
            cmd.runCommands.Add(MapSoundRunCommand(soundData, soundCommandNode));
        }

        private SoundRunCommand MapSoundRunCommand(UISoundData[] soundData, SoundCommandNode soundCommandNode) {
            return new SoundRunCommand(soundCommandNode.isExit, soundCommandNode.runAction) {
                soundData = FindSoundData(soundData, soundCommandNode.name),
            };
        }

        private AnimationData FindAnimationData(in AnimationData[] animations, StyleASTNode animationName) {
            for (int index = 0; index < animations.Length; index++) {
                AnimationData animation = animations[index];
                StyleASTNode value = context.GetValueForReference(animationName);
                if (value is StyleIdentifierNode identifier) {
                    if (animation.name == identifier.name) {
                        return animation;
                    }
                }
                else {
                    throw new CompileException(animationName, $"Could not find an animation with that name or reference: {animationName}");
                }
            }

            throw new CompileException(animationName, $"Could not find an animation with that name or reference: {animationName}");
        }

        private UISoundData FindSoundData(in UISoundData[] soundData, StyleASTNode name) {
            for (int index = 0; index < soundData.Length; index++) {
                UISoundData sound = soundData[index];
                StyleASTNode value = context.GetValueForReference(name);
                if (value is StyleIdentifierNode identifier) {
                    if (sound.name == identifier.name) {
                        return sound;
                    }
                }
                else {
                    throw new CompileException(name, $"Could not find an sound with that name or reference: {name}");
                }
            }

            throw new CompileException(name, $"Could not find an sound with that name or reference: {name}");
        }

        private void MapProperties(AnimationData[] animations, UISoundData[] soundData, ref UIStyleRunCommand targetStyle, LightList<StyleASTNode> styleContainerChildren) {
            for (int i = 0; i < styleContainerChildren.Count; i++) {
                StyleASTNode node = styleContainerChildren[i];
                switch (node) {
                    case PropertyNode propertyNode:
                        // add to normal ui style set
                        StylePropertyMappers.MapProperty(targetStyle.style, propertyNode, context);
                        break;
                    case RunNode runNode:
                        targetStyle.runCommands = targetStyle.runCommands ?? new LightList<IRunCommand>(4);
                        if (runNode.command is AnimationCommandNode animationCommandNode) {
                            MapAnimationCommand(animations, targetStyle, animationCommandNode);
                        }
                        else if (runNode.command is SoundCommandNode soundCommandNode) {
                            MapSoundCommand(soundData, targetStyle, soundCommandNode);
                        }

                        break;
                    default:
                        throw new CompileException(node, $"You cannot have a {node} at this level.");
                }
            }
        }

        private UIStyleRule MapAttributeContainerToRule(ChainableNodeContainer nodeContainer) {
            if (nodeContainer == null) return null;

            if (nodeContainer is AttributeNodeContainer attribute) {
                return new UIStyleRule(attribute.invert, attribute.identifier, attribute.value, MapAttributeContainerToRule(attribute.next));
            }

            if (nodeContainer is ExpressionNodeContainer expression) { }

            throw new NotImplementedException("Sorry this feature experiences a slight delay.");
        }

    }

}