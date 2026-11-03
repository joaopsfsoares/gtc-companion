// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Windows.Forms;
public static class AppLanguageTests {
    public static void Run(){
        using(var panel=new Panel())using(var label=new Label{Text="Servidor"})using(var input=new TextBox{Text="Servidor"})using(var choice=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList}){
            choice.Items.Add("Acelerador");choice.SelectedIndex=0;panel.Controls.Add(label);panel.Controls.Add(input);panel.Controls.Add(choice);AppLanguage.Attach(panel);AppLanguage.Set("en");
            if(label.Text!="Server"||input.Text!="Servidor")throw new Exception("Language switching must translate labels without changing user input.");
            if(Convert.ToString(choice.SelectedItem)!="Acelerador"||choice.GetItemText(choice.SelectedItem)!="Throttle")throw new Exception("Language switching must translate choices without modifying stored values.");
            label.Text="Acelerador";if(label.Text!="Throttle")throw new Exception("Dynamic labels must use the selected language.");
            AppLanguage.Set("pt");if(label.Text!="Acelerador"||input.Text!="Servidor")throw new Exception("Portuguese must be restored without modifying input.");
        }
    }
}
