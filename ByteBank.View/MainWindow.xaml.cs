using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            BtnProcessar.IsEnabled = false;

            _cts = new CancellationTokenSource();

            // Retorna o total de contas a serem processadas.
            var contas = r_Repositorio.GetContaClientes();

            // Obtêm o total de contas que serão processadas e coloca no progressBar.
            PgsProgresso.Maximum = contas.Count();

            // Limpa os controles.
            LimparView();

            // Obtem o horário de início.
            var inicio = DateTime.Now;

            /* reescrever e entender como funciona uma classe Progress do .NET */
            //var byteBankProgress = new ByteBankProgress<String>(str => PgsProgresso.Value++);

            BtnCancelar.IsEnabled = true;

            var progress = new Progress<String>(str => PgsProgresso.Value++);

            try
            {
                // Efetua o processamento em uma thread.
                var resultado = await ConsolidarContas(contas, progress, _cts.Token);

                // Obtêm o horário final do processamento.
                var fim = DateTime.Now;

                // Atualiza os dados para o usuário.
                AtualizarView(resultado, fim - inicio);
            }
            catch (OperationCanceledException)
            {
                TxtTempo.Text = "Operação cancelada pelo usuário";
                PgsProgresso.Value = 0;
            }
            finally
            {
                BtnProcessar.IsEnabled = true;
                BtnCancelar.IsEnabled = false;
                
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelar.IsEnabled = false;

            //Opção para o cancelamento da operação.
            _cts.Cancel();
        }

        private void LimparView()
        {
            LstResultados.ItemsSource = null;
            TxtTempo.Text = null;
            PgsProgresso.Value = 0;
        }

        private async Task<string[]> ConsolidarContas(IEnumerable<ContaCliente> contas,
            IProgress<String> reportadorDeProgresso, CancellationToken ct)
        {

            // Faz o processamento através de Thread.
            var tasks = contas.Select(conta =>
                 Task.Factory.StartNew(() =>
                 {
                     ////Lança a exceção de cancelamento, é neste momento que fazemos a rotina para desfazer o que foi executado.
                     //if (ct.IsCancellationRequested)
                     //    throw new OperationCanceledException(ct);

                     //Caso não for desfazer a alteração e somente cancelar, utiliza a exceção do CancelationToken.
                     ct.ThrowIfCancellationRequested(); //Verifica se a algum cancelamento e gera uma exceção.

                     var resultadoConsolidacao = r_Servico.ConsolidarMovimentacao(conta,ct);

                     // Implementa a interface IProgress.
                     reportadorDeProgresso.Report(resultadoConsolidacao);

                     if (ct.IsCancellationRequested)
                         throw new OperationCanceledException(ct);

                     return resultadoConsolidacao;
                 },ct)
                 );

            return await Task.WhenAll(tasks);
        }

        private void AtualizarView(IEnumerable<string> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count()} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }


    }
}
