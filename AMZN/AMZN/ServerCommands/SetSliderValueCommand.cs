using AMZN.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AMZN.ServerCommands
{
    class SetSliderValueCommand : ICommand
    {
        private readonly ViewModel.ViewModel _viewModel;
        private readonly AMZNServices _service;

        public event EventHandler CanExecuteChanged;

        public SetSliderValueCommand(ViewModel.ViewModel viewModel, AMZNServices service)
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
                await _service.SendSliderValue(Convert.ToInt32(parameter));
            }
            catch
            {
                MessageBox.Show("Unable to send Task!");
            }
        }
    }
}
