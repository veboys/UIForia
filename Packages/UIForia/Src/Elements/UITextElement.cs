using System;
using UIForia.Attributes;
using UIForia.ListTypes;
using UIForia.Text;
using UIForia.Util.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace UIForia.Elements {

    [TemplateTagName("Text")]
    public unsafe class UITextElement : UIElement {

        internal string text;
        internal ITextProcessor _processor;
        internal TextInfo* textInfo;
        internal int lastUpdateFrame;

        ~UITextElement() {
            OnDestroy();
        }

        private void EnsureTextInfo() {
            if (textInfo != null) {
                return;
            }

            textInfo = TypedUnsafe.Malloc<TextInfo>(1, Allocator.Persistent);
            *textInfo = default;
        }

        public override void OnCreate() {
            EnsureTextInfo();
        }

        public override void OnDestroy() {
            if (textInfo == null) {
                return;
            }

            textInfo->Dispose();
            TypedUnsafe.Dispose(textInfo, Allocator.Persistent);
            textInfo = null;
        }

        // setting this and text in same frame will run update twice, improve this
        public ITextProcessor processor {
            get => _processor;
            set {
                if (_processor != value) {
                    _processor = value;
                    UpdateText();
                }
            }
        }

        public bool HasSelection {
            get { return textInfo != null && textInfo->HasSelection; }
        }

        public string GetText() {
            return text;
        }

        private void UpdateText() {
            EnsureTextInfo();
            application.textSystem.UpdateText(this);
        }

        public void SetTextFromCharacters(char[] newText, int length) {
            if (text == null || text.Length != length) {
                // needs update
                text = new string(newText, 0, length);
                UpdateText();
                return;
            }

            fixed (char* oldTextPtr = text)
            fixed (char* newTextPtr = newText) {
                if (UnsafeUtility.MemCmp(oldTextPtr, newTextPtr, 2 * length) != 0) {
                    text = new string(newText, 0, length);
                    UpdateText();
                }
            }
        }

        public void SetText(string newText) {
            if (newText == null) newText = string.Empty;

            if (text == null || text.Length != newText.Length) {
                // needs update
                text = newText;
                UpdateText();
                return;
            }

            fixed (char* oldTextPtr = text)
            fixed (char* newTextPtr = newText) {
                if (UnsafeUtility.MemCmp(oldTextPtr, newTextPtr, 2 * newText.Length) != 0) {
                    text = newText;
                    UpdateText();
                }
            }
        }

        public override string GetDisplayName() {
            return "Text";
        }

        public void SelectWordAtPoint(Vector2 point) {
            if (textInfo == null) return;
            textInfo->SelectWordAtPoint(point);
        }

        public void SelectLineAtPoint(Vector2 point) {
            if (textInfo == null) return;
            textInfo->SelectLineAtPoint(point);
        }

        public bool FontHasCharacter(char c) {
            if (textInfo == null) return false;

            // todo -- search fallback fonts
            // todo -- because of rich text, we need to know the 'active' font to search

            if (application.textSystem.TryGetFontAsset(textInfo->fontAssetId, out FontAssetInfo fontAssetInfo)) {
                return fontAssetInfo.TryGetGlyph((int) c, out UIForiaGlyph glyph);
            }

            return false;
        }

        public float2 GetCursorPosition(int cursorIndex) {
            if (textInfo == null) return default;

            List_TextSymbol list = textInfo->symbolList;

            int charIdx = 0;
            for (int i = 0; i < list.size; i++) {
                ref TextSymbol symbol = ref list.array[i];
                if (symbol.type != TextSymbolType.Character) {
                    continue;
                }

                if (charIdx == cursorIndex) {
                    ref BurstCharInfo charInfo = ref symbol.charInfo;
                    if (charInfo.wordIndex >= textInfo->layoutSymbolList.size) {
                        return default;
                    }

                    ref WordInfo wordInfo = ref textInfo->layoutSymbolList.array[charInfo.wordIndex].wordInfo;
                    return charInfo.position + new float2(wordInfo.x, wordInfo.y);
                }

                charIdx++;
            }

            return default;
        }

        public void MoveToStartOfLine(bool evtShift) {
            if (textInfo == null) return;
            textInfo->MoveToStartOfLine(evtShift);
        }

        public SelectionRange MoveToEndOfLine(SelectionRange selectionRange, bool evtShift) {
            throw new NotImplementedException();
        }

        public void MoveCursorLeft(bool maintainSelection, bool evtCommand) {
            if (textInfo == null) return;
            textInfo->MoveCursorLeft(maintainSelection, evtCommand);
        }

        public void MoveCursorRight(bool maintainSelection, bool evtCommand) {
            if (textInfo == null) return;
            textInfo->MoveCursorRight(maintainSelection, evtCommand);
        }

        public string GetSelectedString() {
            if (textInfo == null) return string.Empty;
            return textInfo->GetSelectedText();
        }

        public int GetIndexAtPoint(Vector2 point) {
            if (textInfo == null) return -1;
            return textInfo->GetIndexAtPoint(application.ResourceManager.fontAssetMap, point);
        }

        public SelectionCursor GetSelectionCursorAtPoint(Vector2 point) {
            if (textInfo == null) return new SelectionCursor();
            return textInfo->GetSelectionCursorAtPoint(application.ResourceManager.fontAssetMap, point);
        }

        public Rect GetCursorRect() {
            if (textInfo == null) return default;
            return textInfo->GetCursorRect(application.ResourceManager.fontAssetMap);
        }

        public void SetSelection(SelectionCursor cursor, SelectionCursor selection) {
            if (textInfo == null) return;
            textInfo->selectionCursor = cursor;
            textInfo->selectionOrigin = selection;
        }

        public SelectionCursor GetSelectionStartCursor() {
            if (textInfo == null) return default;
            return textInfo->selectionCursor;
        }

        public SelectionCursor GetSelectionEndCursor() {
            if (textInfo == null) return default;
            return textInfo->selectionOrigin;
        }

        public string InsertText(string text, string characters) {
            if (textInfo == null) return default;
            return SelectionRangeUtil.InsertText(ref UnsafeUtility.AsRef<TextInfo>(textInfo), text, characters);
        }

        public string DeleteForwards() {
            if (textInfo == null) return text;
            SetText(SelectionRangeUtil.DeleteTextForwards(text, ref UnsafeUtility.AsRef<TextInfo>(textInfo)));
            return text;
        }

        public string DeleteBackwards() {
            if (textInfo == null) return text;
            SetText(SelectionRangeUtil.DeleteTextBackwards(text, ref UnsafeUtility.AsRef<TextInfo>(textInfo)));
            return text;
        }

        public void SelectAll() {
            if (textInfo == null) return;
            textInfo->_selectionOrigin = new SelectionCursor(0, SelectionEdge.Left);
            textInfo->selectionCursor = new SelectionCursor(textInfo->symbolList.size - 1, SelectionEdge.Right);
        }

    }

    public struct SelectionCursor {

        public int index;
        public SelectionEdge edge;

        public SelectionCursor(int index, SelectionEdge edge) {
            this.index = index;
            this.edge = edge;
        }

        public static SelectionCursor Invalid => new SelectionCursor(-1, SelectionEdge.Left);
        public bool IsInvalid => index < 0;
        public bool IsValid => index >= 0;

    }

}