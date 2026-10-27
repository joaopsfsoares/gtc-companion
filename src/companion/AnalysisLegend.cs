// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System.Drawing;

public static class AnalysisLegend {
    public const int Height=90;
    public static readonly Color Green=Color.FromArgb(83,218,154),Red=Color.FromArgb(255,112,126);
    public static Color Point(bool clean,double value,double? reference,double? tolerance=null){return !clean?GtcTheme.Muted:!reference.HasValue?GtcTheme.Text:(tolerance.HasValue?TrainingMath.WithinWindow(value,reference.Value,tolerance.Value):value<=reference.Value)?Green:Red;}
    public static void Draw(Graphics g,float width,float top,Font font,string reference,bool compared,bool pits,bool window=false){
        float left=18,right=width/2;using(var ink=new SolidBrush(GtcTheme.Text))using(var muted=new SolidBrush(GtcTheme.Muted))using(var accent=new Pen(GtcTheme.Accent,2)){
            g.DrawLine(accent,left,top+8,left+16,top+8);g.DrawString(reference,font,ink,left+24,top);
            if(compared){using(var green=new SolidBrush(Green))g.FillEllipse(green,left,top+24,7,7);g.DrawString(window?"Dentro do intervalo aceitável":"Igual ou mais rápido que a referência",font,ink,left+14,top+20);using(var red=new SolidBrush(Red))g.FillEllipse(red,right,top+24,7,7);g.DrawString(window?"Fora do intervalo aceitável":"Mais lento que a referência",font,ink,right+14,top+20);}
            else{using(var dot=new SolidBrush(GtcTheme.Text))g.FillEllipse(dot,left,top+24,7,7);g.DrawString("Volta válida · sem comparação de tempo",font,ink,left+14,top+20);}
            g.DrawString("×",font,ink,left-2,top+40);g.DrawString("Inválida / dados incompletos",font,ink,left+14,top+40);
            if(pits)g.DrawString("PIT  Boxes · ~ limite aproximado",font,ink,right,top+40);else{using(var pen=new Pen(GtcTheme.Accent,1.5f))g.DrawEllipse(pen,right,top+44,9,9);g.DrawString("Saída de pista estimada",font,ink,right+16,top+40);}
            g.DrawString(pits?"○ Branco: volta selecionada · Sem ponto: tempo em falta.":"Voltas e setores sem tempo disponível ficam sem ponto.",font,muted,left,top+62);
        }
    }
}
