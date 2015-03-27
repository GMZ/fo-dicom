// 
// Copyright (c) 2004-2011 Jaroslaw Kowalski <jaak@jkowalski.net>
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using NLog.Config;
using System.Windows.Documents;
using System.Windows.Media;
using Application = System.Windows.Application;
using RichTextBox = System.Windows.Controls.RichTextBox;

#if !NET_CF && !MONO && !SILVERLIGHT

namespace NLog.Targets
{


    [Target("RichTextBox")]
    public sealed class WpfRichTextBoxTarget : TargetWithLayout
    {
        private static readonly TypeConverter ColorConverter = new ColorConverter();
        private static Color _bgColor = Color.FromRgb(255, 255, 255);
        private int _lineCount;

        static WpfRichTextBoxTarget()
        {
            var rules = new List<WpfRichTextBoxRowColoringRule>
                            {
                                new WpfRichTextBoxRowColoringRule("level == LogLevel.Fatal", "White", "Red", FontStyles.Normal, FontWeights.Bold),
                                new WpfRichTextBoxRowColoringRule("level == LogLevel.Error", "Red", "Empty", FontStyles.Italic, FontWeights.Bold),
                                new WpfRichTextBoxRowColoringRule("level == LogLevel.Warn", "Orange", "Empty"),
                                new WpfRichTextBoxRowColoringRule("level == LogLevel.Info", "Lime", "Empty"),
                                new WpfRichTextBoxRowColoringRule("level == LogLevel.Debug", "Gray", "Empty"),
                                new WpfRichTextBoxRowColoringRule("level == LogLevel.Trace", "DarkGray", "Empty", FontStyles.Italic, FontWeights.Normal),
                            };

            DefaultRowColoringRules = rules.AsReadOnly();
        }

        public WpfRichTextBoxTarget(Color color)
        {
            _bgColor = color;
            WordColoringRules = new List<WpfRichTextBoxWordColoringRule>();
            RowColoringRules = new List<WpfRichTextBoxRowColoringRule>();
            ToolWindow = true;
        }

        public static ReadOnlyCollection<WpfRichTextBoxRowColoringRule> DefaultRowColoringRules { get; private set; }

        public string ControlName { get; set; }

        public string FormName { get; set; }

        [DefaultValue(false)]
        public bool UseDefaultRowColoringRules { get; set; }

        [ArrayParameter(typeof(WpfRichTextBoxRowColoringRule), "row-coloring")]
        public IList<WpfRichTextBoxRowColoringRule> RowColoringRules { get; private set; }

        [ArrayParameter(typeof(WpfRichTextBoxWordColoringRule), "word-coloring")]
        public IList<WpfRichTextBoxWordColoringRule> WordColoringRules { get; private set; }

        [DefaultValue(true)]
        public bool ToolWindow { get; set; }

        public bool ShowMinimized { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public bool AutoScroll { get; set; }

        public int MaxLines { get; set; }

        internal Form TargetForm { get; set; }

        internal RichTextBox TargetRichTextBox { get; set; }

        internal bool CreatedForm { get; set; }

        protected override void InitializeTarget()
        {
            TargetRichTextBox = (RichTextBox)Application.Current.MainWindow.FindName(ControlName);
        }

        protected override void CloseTarget()
        {
            if (CreatedForm)
            {
                TargetForm.Invoke((FormCloseDelegate)TargetForm.Close);
                TargetForm = null;
            }
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var matchingRule = RowColoringRules.FirstOrDefault(rr => rr.CheckCondition(logEvent));

            if (UseDefaultRowColoringRules && matchingRule == null)
            {
                foreach (var rr in DefaultRowColoringRules.Where(rr => rr.CheckCondition(logEvent)))
                {
                    matchingRule = rr;
                    break;
                }
            }

            if (matchingRule == null)
            {
                matchingRule = WpfRichTextBoxRowColoringRule.Default;
            }

            var logMessage = Layout.Render(logEvent);

            if (Application.Current.Dispatcher.CheckAccess() == false)
            {
                Application.Current.Dispatcher.Invoke(new Action(() => SendTheMessageToRichTextBox(logMessage, matchingRule)));
            }
            else
            {
                SendTheMessageToRichTextBox(logMessage, matchingRule);
            }
        }

        private static Color GetColorFromString(string color, Brush defaultColor)
        {
            if (defaultColor == null)
            {
                return _bgColor;
            }
            if (color == "Empty")
            {
                return _bgColor;
            }

            return (Color)ColorConverter.ConvertFromString(color);
        }

        private void SendTheMessageToRichTextBox(string logMessage, WpfRichTextBoxRowColoringRule rule)
        {
            var rtbx = TargetRichTextBox;

            logMessage = logMessage.Replace("\n", "\r");
            var tr = new TextRange(rtbx.Document.ContentEnd, rtbx.Document.ContentEnd)
            {
                Text = logMessage + "\r"
            };
            tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                new SolidColorBrush(GetColorFromString(rule.FontColor, (Brush)tr.GetPropertyValue(TextElement.ForegroundProperty)))
            );
            tr.ApplyPropertyValue(TextElement.BackgroundProperty,
                new SolidColorBrush(GetColorFromString(rule.BackgroundColor, (Brush)tr.GetPropertyValue(TextElement.BackgroundProperty)))
            );
            tr.ApplyPropertyValue(TextElement.FontStyleProperty, rule.Style);
            tr.ApplyPropertyValue(TextElement.FontWeightProperty, rule.Weight);

            if (MaxLines > 0)
            {
                _lineCount++;
                if (_lineCount > MaxLines)
                {
                    tr = new TextRange(rtbx.Document.ContentStart, rtbx.Document.ContentEnd);
                    tr.Text.Remove(0, tr.Text.IndexOf('\n'));
                    _lineCount--;
                }
            }

            if (AutoScroll)
            {
                rtbx.ScrollToEnd();
            }
        }

        private delegate void DelSendTheMessageToRichTextBox(string logMessage, WpfRichTextBoxRowColoringRule rule);

        private delegate void FormCloseDelegate();
    }
}
#endif