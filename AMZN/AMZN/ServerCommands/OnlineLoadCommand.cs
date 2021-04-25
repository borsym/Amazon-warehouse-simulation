using AMZN.Persistence;
using AMZN.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AMZN.ServerCommands
{
    class OnlineLoadCommand : ICommand
    {
        private readonly ViewModel.ViewModel _viewModel;
        private readonly AMZNServices _service;

        public event EventHandler CanExecuteChanged;

        public OnlineLoadCommand(ViewModel.ViewModel viewModel, AMZNServices service)
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
            try
            {
                string data = (string)parameter;
                await _service.LoadOnline(data);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message.ToString());
                MessageBox.Show("Unable to send Task!");
            }
        }
    }
}
