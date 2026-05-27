using BankApp.Models;
using BankApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace BankApp.ViewModels
{
    public class HelpViewModel : BaseViewModel
    {
        public ObservableCollection<FaqItem> Faqs { get; set; }
        public RelayCommand GoBackCommand { get; set; }

        public HelpViewModel()
        {
            GoBackCommand = new RelayCommand(GoBack);
            Faqs = new ObservableCollection<FaqItem>();

            LoadFaq();
        }

        private void GoBack()
        {
            try
            {
                NavService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка навигации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadFaq()
        {
            // CLIENT
            if (Session.IsClient)
            {
                Faqs.Add(new FaqItem
                {
                    Question = "Как выполнить перевод?",
                    Answer =
                    "Перейдите в раздел 'Переводы', выберите счет и укажите IBAN или номер телефона получателя, затем введите сумму перевода."
                });

                Faqs.Add(new FaqItem
                {
                    Question = "Как пополнить счет?",
                    Answer =
                    "Откройте раздел пополнения, выберите счет, вашу карту, далее введите сумму пополнения."
                });

                Faqs.Add(new FaqItem
                {
                    Question = "Как посмотреть историю операций?",
                    Answer =
                    "Все операции доступны на главной странице и в разделе операций."
                });

                Faqs.Add(new FaqItem
                {
                    Question = "Что делать при подозрительной активности?",
                    Answer =
                    "Откройте раздел безопасности и завершите активные сессии."
                });
            }

            // OPERATOR
            else if (Session.IsOperator)
            {
                Faqs.Add(new FaqItem
                {
                    Question = "Как открыть счет клиенту?",
                    Answer =
                    "Перейдите в раздел счетов, выберите и введите данные для нового счета и нажмите кнопку 'Создать'."
                });

                Faqs.Add(new FaqItem
                {
                    Question = "Как отменить операцию?",
                    Answer =
                    "В разделе операций выберите нужную транзакцию и нажмите 'Отменить'."
                });

                Faqs.Add(new FaqItem
                {
                    Question = "Как экспортировать операции?",
                    Answer =
                    "Используйте кнопки PDF или Excel над таблицей."
                });
            }

            // ADMIN
            else if (Session.IsAdmin)
            {
                Faqs.Add(new FaqItem
                {
                    Question = "Как заблокировать пользователя?",
                    Answer =
                    "Откройте раздел пользователей и нажмите кнопку блокировки."
                });

                Faqs.Add(new FaqItem
                {
                    Question = "Где посмотреть системные логи?",
                    Answer =
                    "Все действия отображаются в панели администратора."
                });

                Faqs.Add(new FaqItem
                {
                    Question = "Как назначить роль оператору?",
                    Answer =
                    "Измените роль пользователя в таблице управления."
                });
            }
        }
    }
}