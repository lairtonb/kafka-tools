using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MVVMTest01
{
    public class AddItemCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            // PatientManager patientManager = new PatientManager();
            // PatientDetailModel patientDetail = parameter as PatientDetailModel;

            // if (patientManager.Add(new PatientDetailModel { Id = patientDetail.Id, Name = patientDetail.Name }))
            //    MessageBox.Show("Patient Add Successful !");
            //else
            //    MessageBox.Show("Patient with this ID already exists !");
        }
    }
}
