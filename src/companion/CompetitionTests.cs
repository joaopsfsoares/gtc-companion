// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

public static class CompetitionTests {
    public static void Run(){
        string previous=CompanionStore.Root;string root=Path.Combine(previous,"profile-tests-"+Guid.NewGuid().ToString("N"));CompanionStore.Root=root;
        try{
            CompanionStore.Write("settings.json",new{Configured=true,Driver="Piloto exemplo",AutoStart=false,ServerHost="old.example.test",ServerPort=12345,ServerPassword="private-test-only",TimingId=110});
            Directory.CreateDirectory(CompetitionProfiles.PreferencesDirectory);File.WriteAllText(Path.Combine(CompetitionProfiles.PreferencesDirectory,"personal-records.json"),"{\"test\":true}");
            File.WriteAllText(Path.Combine(CompetitionProfiles.PreferencesDirectory,"settings.json"),"{\"OpacityPercent\":77,\"X\":321,\"Y\":456}");
            var migrated=CompetitionProfiles.Load(true);var first=CompetitionProfiles.Selected(migrated);
            if(first.Name!="Porsche 911 Cup"||migrated.ServerPassword!="private-test-only"||migrated.Driver!="Piloto exemplo"||migrated.AutoStart)throw new Exception("Migração do campeonato e preferências");
            string firstDirectory=CompetitionProfiles.DataDirectory(first);
            if(!File.Exists(Path.Combine(firstDirectory,"personal-records.json"))||File.Exists(Path.Combine(firstDirectory,"settings.json")))throw new Exception("Migração tem de separar tempos e preferências comuns");
            var again=CompetitionProfiles.Load(true);if(again.Competitions.Count!=1||again.Competitions[0].Id!=first.Id)throw new Exception("Migração duplicada");
            var second=new CompetitionProfile{Name="Outro campeonato",TimingId=110,ServerHost="second.example.test",ServerPort=23456,ServerPassword="second-only"};migrated.Competitions.Add(second);
            if(CompetitionProfiles.DataDirectory(second)==firstDirectory)throw new Exception("Tabelas iguais em perfis diferentes têm de manter históricos separados");
            migrated.SelectedCompetitionId=second.Id;if(!migrated.JoinUrl.Contains("second.example.test")||migrated.ServerPassword!="second-only")throw new Exception("Link do campeonato selecionado");
            second.Name="Novo nome";if(CompetitionProfiles.DataDirectory(second)==firstDirectory||CompetitionProfiles.ValidateProfile(migrated,second)!=null)throw new Exception("Renomear campeonato");
            var duplicate=new CompetitionProfile{Name="  novo nome  "};if(CompetitionProfiles.ValidateProfile(migrated,duplicate)==null)throw new Exception("Nomes duplicados");
            if(CompetitionProfiles.ParseTimingLink("")!=0||CompetitionProfiles.ParseTimingLink("https://esports.gtcracecenter.com/livetiming/123")!=123)throw new Exception("Ligação da classificação");
            foreach(string invalid in new[]{"https://example.test/livetiming/110","https://esports.gtcracecenter.com/comp/foo","-1"}){bool rejected=false;try{CompetitionProfiles.ParseTimingLink(invalid);}catch(ArgumentException){rejected=true;}if(!rejected)throw new Exception("Classificação inválida aceite");}
            var connection=new CompetitionConnection{ProfileId=first.Id,Host=first.ServerHost,Port=first.ServerPort,GameStamp="test-game"};
            if(!CompetitionProfiles.SameConnection(connection,first,"test-game")||CompetitionProfiles.SameConnection(connection,second,"test-game")||CompetitionProfiles.SameConnection(connection,first,"another-game"))throw new Exception("Confirmação da ligação entre campeonatos");
            CompanionStore.Write("settings.json",migrated);again=CompetitionProfiles.Load(true);
            if(again.SelectedCompetitionId!=second.Id||again.Competitions.Count!=2||again.ServerPassword!="second-only")throw new Exception("Persistência de vários campeonatos");
            string serialized=File.ReadAllText(Path.Combine(root,"settings.json"));if(serialized.Split(new[]{"second-only"},StringSplitOptions.None).Length!=2)throw new Exception("Dados privados duplicados fora do perfil");
            using(var window=new CompanionWindow(again,true)){window.PreviewOnly=true;window.VerifyCompetitionSelection(first.Id,second.Id);}
            if(!File.Exists(Path.Combine(firstDirectory,"personal-records.json")))throw new Exception("Eliminar o perfil deve preservar o histórico local");
            var reloaded=CompetitionProfiles.Load(true);if(reloaded.Competitions.Count!=0||CompetitionProfiles.Selected(reloaded)!=null)throw new Exception("O último campeonato eliminado não pode reaparecer após reiniciar");
            string deletedSettings=File.ReadAllText(Path.Combine(root,"settings.json"));if(deletedSettings.Contains("private-test-only")||deletedSettings.Contains("second-only"))throw new Exception("O acesso eliminado não pode permanecer nas definições ativas");
            if(CompetitionProfiles.Remove(reloaded,Guid.NewGuid().ToString("N")))throw new Exception("Eliminar um perfil inexistente");
        }finally{CompanionStore.Root=previous;}
    }
}
public sealed partial class CompanionWindow {
    public void VerifyCompetitionSelection(string first,string second){
        if(!PreviewOnly)throw new InvalidOperationException();
        competitions.SelectedItem=settings.Competitions.First(p=>p.Id==first);
        if(settings.SelectedCompetitionId!=first||settings.ServerHost!="old.example.test"||(int)timing.Value!=110)throw new Exception("Seleção do primeiro campeonato na interface");
        competitions.SelectedItem=settings.Competitions.First(p=>p.Id==second);
        if(settings.SelectedCompetitionId!=second||settings.ServerHost!="second.example.test"||linkEditing)throw new Exception("Seleção do segundo campeonato na interface");
        var empty=new CompetitionProfile{Name="Sem servidor"};settings.Competitions.Add(empty);RefreshCompetitions();competitions.SelectedItem=empty;
        if(settings.TimingId!=0||!linkEditing||competitionTable.Enabled)throw new Exception("Perfil sem classificação ou servidor não pode herdar os do Porsche");
        competitions.SelectedItem=settings.Competitions.First(p=>p.Id==first);
        if(settings.ServerPassword!="private-test-only"||settings.TimingId!=110)throw new Exception("Voltar ao campeonato tem de preservar o acesso");
        var candidate=new CompetitionProfile{Name=""};using(var editor=CreateCompetitionEditor(true,candidate)){
            editor.StartPosition=FormStartPosition.Manual;editor.Location=new System.Drawing.Point(-20000,-20000);editor.Show();Application.DoEvents();
            var panel=(TableLayoutPanel)editor.Controls[0];var fields=panel.Controls.OfType<TextBox>().ToArray();var actions=panel.Controls.OfType<FlowLayoutPanel>().Single();var add=actions.Controls.OfType<Button>().First();
            add.PerformClick();if(editor.DialogResult==DialogResult.OK)throw new Exception("O formulário não pode aceitar um nome vazio");
            fields[0].Text="Campeonato de teste";fields[2].Text="https://example.test/livetiming/99";add.PerformClick();if(editor.DialogResult==DialogResult.OK)throw new Exception("O formulário aceitou uma fonte inválida");
            fields[2].Text="https://esports.gtcracecenter.com/livetiming/99";add.PerformClick();if(editor.DialogResult!=DialogResult.OK||candidate.Name!="Campeonato de teste"||candidate.TimingId!=99)throw new Exception("Adicionar campeonato pela interface");editor.Close();
        }
        int count=settings.Competitions.Count;
        using(var editor=CreateCompetitionEditor(false,CompetitionProfiles.Selected(settings).Copy())){
            var panel=(TableLayoutPanel)editor.Controls[0];var actions=panel.Controls.OfType<FlowLayoutPanel>().Single();if(!actions.Controls.OfType<Button>().Any(b=>b.Name=="DeleteCompetition"))throw new Exception("Falta o botão Eliminar nas definições do campeonato");
            editor.DialogResult=DialogResult.Cancel;
        }
        if(settings.Competitions.Count!=count)throw new Exception("Cancelar a edição não pode eliminar campeonatos");
        RemoveCompetition(CompetitionProfiles.Selected(settings));
        if(settings.Competitions.Count!=count-1||settings.SelectedCompetitionId!=second||settings.ServerPassword!="second-only")throw new Exception("Eliminar o campeonato selecionado deve selecionar o seguinte");
        RemoveCompetition(settings.Competitions.Single(p=>p.Name=="Sem servidor"));
        if(settings.SelectedCompetitionId!=second)throw new Exception("Eliminar outro perfil não pode mudar a seleção");
        RemoveCompetition(CompetitionProfiles.Selected(settings));
        if(settings.Competitions.Count!=0||start.Enabled||editCompetition.Enabled||timing.Enabled||serverLink.Enabled||competitionTable.Enabled)throw new Exception("Estado vazio após eliminar o último campeonato");
        if(!menuStart.Enabled||AcceptButton!=menuStart)throw new Exception("Abrir sem servidor deve continuar disponível sem campeonatos");
        serverLink.Text="rascunho inválido";
        if(!Save(false,true)||serverLink.Text!="rascunho inválido"||settings.Competitions.Count!=0)throw new Exception("Arranque normal não pode exigir um link nem apagar um rascunho");
        if(!Save(false)||settings.Competitions.Count!=0)throw new Exception("Guardar as preferências gerais não pode recriar o campeonato eliminado");
        var fresh=new CompetitionProfile{Name="Novo campeonato"};settings.Competitions.Add(fresh);settings.SelectedCompetitionId=fresh.Id;SyncCompetition();
        if(!start.Enabled||!editCompetition.Enabled||!linkEditing||settings.ServerHost!=""||settings.TimingId!=0)throw new Exception("Adicionar um campeonato depois de esvaziar a lista");
        RemoveCompetition(fresh);
    }
}
