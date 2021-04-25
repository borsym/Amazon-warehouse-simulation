using AMZN.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AMZN.ServerCommands
{
    class SendOnlineOrderCommand : ICommand
    {
        private readonly ViewModel.ViewModel _viewModel;
        private readonly AMZNServices _service;

        public event EventHandler CanExecuteChanged;

        public SendOnlineOrderCommand(ViewModel.ViewModel viewModel, AMZNServices service)
        {
            _viewModel = viewModel;
            _service = service;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }


        public async void Execute(object parameter)
        {
            OnlineOrder order = (OnlineOrder) parameter;
            try
            {
               // MessageBox.Show("küldi");
                await _service.SendOnlineOrder(order);
            }
            catch
            {
                MessageBox.Show("Unable to send Task!");
            }
        }
    }
}
