using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();
            BtnProcessar.IsEnabled = false;

            var contas = r_Repositorio.GetContaClientes();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;

            var resultado = await ConsolidarContas(contas);

            var fim = DateTime.Now;
            AtualizarView(resultado, fim - inicio);
            BtnProcessar.IsEnabled = true;

        }


        private async Task<string[]> ConsolidarContas(IEnumerable<ContaCliente> contas)
        {
            var resultado = new List<string>();

            var tasks = contas.Select(conta =>
                 Task.Factory.StartNew(() =>   r_Servico.ConsolidarMovimentacao(conta)));

            return await Task.WhenAll(tasks);
        }
        private void AtualizarView(IEnumerable<string> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count()} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }

        #region Métodos das aulas anteriores
        private void BtnProcessar_Click_1(object sender, RoutedEventArgs e)
        {
            var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();
            BtnProcessar.IsEnabled = false;

            var contas = r_Repositorio.GetContaClientes();

            var resultado = new List<string>();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;


            // Task.Factory.StartNew -> Gerencia as thread automáticamente.
            var contasTarefas = contas.Select(conta =>
            {
                return Task.Factory.StartNew(() =>
                {
                    var resultadoConta = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoConta);
                });
            }).ToArray();

            //Task.WaitAll(contasTarefas); // Para a aplicação e espera até que todas thread termine.

            Task.WhenAll(contasTarefas) // Consiste em retornar uma tarefa que espera o término de todas as demais recebidas por parâmetro
                .ContinueWith(task =>
                {
                    var fim = DateTime.Now;
                    AtualizarView(resultado, fim - inicio);
                }, taskSchedulerUI)
                .ContinueWith(task =>
                {
                    BtnProcessar.IsEnabled = true;
                }, taskSchedulerUI);

        }

        private void BtnProcessar_Click_2(object sender, RoutedEventArgs e)
        {
            var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();
            BtnProcessar.IsEnabled = false;

            var contas = r_Repositorio.GetContaClientes();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;

            //Task.WaitAll(contasTarefas); // Para a aplicação e espera até que todas thread termine.

            ConsolidarContas(contas) // Consiste em retornar uma tarefa que espera o término de todas as demais recebidas por parâmetro
                .ContinueWith(task =>
                {
                    var fim = DateTime.Now;
                    var resultado = task.Result;
                    AtualizarView(resultado, fim - inicio);
                }, taskSchedulerUI)
                .ContinueWith(task =>
                {
                    BtnProcessar.IsEnabled = true;
                }, taskSchedulerUI);

        }
        #endregion
    }
}
