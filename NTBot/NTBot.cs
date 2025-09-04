#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NTBot;
#endregion

// Este namespace contém os elementos da GUI e é obrigatório.
namespace NinjaTrader.Gui.NinjaScript
{
    // O NinjaTrader cria uma instância de cada classe derivada de "AddOnBase"
    public class NTBot : AddOnBase
    {
        private NTMenuItem ntBotMenuItem;
        private NTMenuItem existingMenuItemInControlCenter;

        // Configura as propriedades padrão do AddOn
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Bot para execução automática de estratégias de trading";
                Name = "NT Bot";
            }
        }

        // É chamado quando uma nova janela do NinjaTrader é criada
        protected override void OnWindowCreated(Window window)
        {
            // Queremos colocar nosso AddOn nos menus do Control Center
            ControlCenter cc = window as ControlCenter;
            if (cc == null)
                return;

            // Determina onde queremos colocar nosso AddOn no menu "New" do Control Center
            existingMenuItemInControlCenter = cc.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
            if (existingMenuItemInControlCenter == null)
                return;

            // 'Header' define o nome do nosso AddOn visto na estrutura do menu
            ntBotMenuItem = new NTMenuItem { Header = "NT Bot", Style = Application.Current.TryFindResource("MainMenuItem") as Style };

            // Adiciona nosso AddOn ao menu "New"
            existingMenuItemInControlCenter.Items.Add(ntBotMenuItem);

            // Inscreve-se no evento quando o usuário pressiona o item de menu do nosso AddOn
            ntBotMenuItem.Click += OnMenuItemClick;
        }

        // É chamado quando uma janela do NinjaTrader é destruída
        protected override void OnWindowDestroyed(Window window)
        {
            if (ntBotMenuItem != null && window is ControlCenter)
            {
                if (existingMenuItemInControlCenter != null && existingMenuItemInControlCenter.Items.Contains(ntBotMenuItem))
                    existingMenuItemInControlCenter.Items.Remove(ntBotMenuItem);

                ntBotMenuItem.Click -= OnMenuItemClick;
                ntBotMenuItem = null;
            }
        }

        // Abre a janela do nosso AddOn quando o item de menu é clicado
        private void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            Core.Globals.RandomDispatcher.BeginInvoke(new Action(() => new NTBotWindow().Show()));
        }
    }

    // Factory para criação de abas na janela do NT Bot
    public class NTBotWindowFactory : INTTabFactory
    {
        // Membro INTTabFactory. Necessário para criar a janela principal
        public NTWindow CreateParentWindow()
        {
            return new NTBotWindow();
        }

        // Membro INTTabFactory. Necessário para criar abas
        public NTTabPage CreateTabPage(string typeName, bool isTrue)
        {
            return new NTBotPage();
        }
    }

    // Define a janela principal do NT Bot
    public class NTBotWindow : NTWindow, IWorkspacePersistence
    {
        public NTBotWindow()
        {
            // Define o título da janela
            Caption = "NT Bot";

            // Define as dimensões padrão da janela
            Width = 1085;
            Height = 900;

            // Cria um controle de abas para o conteúdo da janela
            TabControl tc = new TabControl();

            // Configura as propriedades para permitir mover, adicionar e remover abas
            TabControlManager.SetIsMovable(tc, true);
            TabControlManager.SetCanAddTabs(tc, true);
            TabControlManager.SetCanRemoveTabs(tc, true);

            // Define a factory para criar novas abas
            TabControlManager.SetFactory(tc, new NTBotWindowFactory());
            Content = tc;

            // Adiciona a página principal do NT Bot
            tc.AddNTTabPage(new NTBotPage());

            // Configura opções de workspace
            Loaded += (o, e) =>
            {
                if (WorkspaceOptions == null)
                    WorkspaceOptions = new WorkspaceOptions("NTBot-" + Guid.NewGuid().ToString("N"), this);
            };
        }

        // Membro IWorkspacePersistence. Necessário para restaurar a janela do workspace
        public void Restore(XDocument document, XElement element)
        {
            if (MainTabControl != null)
                MainTabControl.RestoreFromXElement(element);
        }

        // Membro IWorkspacePersistence. Necessário para salvar a janela no workspace
        public void Save(XDocument document, XElement element)
        {
            if (MainTabControl != null)
                MainTabControl.SaveToXElement(element);
        }

        // Membro IWorkspacePersistence
        public WorkspaceOptions WorkspaceOptions
        { get; set; }
    }
}
