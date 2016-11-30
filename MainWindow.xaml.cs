/****************************************************************************************** 
 *	WPrime                                   											  *
 *	Copyright 2016 - Barna Birtók - PcSpaceGeep									   		  *
 *																						  *
 *	WPrime is free software: you can redistribute it and/or modify	                      *
 *	it under the terms of the GNU General Public License as published by				  *
 *	the Free Software Foundation, either version 3 of the License, or					  *
 *	(at your option) any later version.													  *
 *																						  *
 *	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,   *
 *	INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A         *
 *	PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT    *
 *	HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF  *
 *	CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE  *
 *	OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.									      *
 ******************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace WPrime {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        // Declare a bunch of variables, which needs to be accesed by multipple methods
        decimal a1, a2;

        int b1, b2;
        int nrOfCores;
        int finishedCores;

        ulong primeStorageLimiter = 100000000;
        ulong startNumber, endNumber;
        ulong smallStartNumber, smallEndNumber;
        ulong numberOfPrimesFound;
        List<ulong> progressIndicator = new List<ulong>();
        
        List<ulong> primelist = new List<ulong>();
        List<ulong> newPrimes = new List<ulong>();
        
        Thread[] threads;
        Thread mainTasker;
        Thread ub;

        Stopwatch sw = new Stopwatch();

        string folderName = "";


        public MainWindow() {
            InitializeComponent();

            // Create the optins for usable processor comboBox
            for (int i = Environment.ProcessorCount; i > 0 ; i--) {
                CPUcomboBox.Items.Add(i);
            }
            CPUcomboBox.SelectedValue = Environment.ProcessorCount;
        }

        private void CalculateButtonClick(object sender, RoutedEventArgs e) {

            // Create the new folder (if it was requested)

            if (checkBox.IsChecked.Value == true) {
                folderName = DateTime.Now.ToString("yyyy/MM/dd - hh-mm");
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), folderName));
            }

            // Check if the supplied numbers are correct

            bool correctImput = ImputVerification();
            if (correctImput) {

                // Check and load the prime imput file

                bool loadedFile = LoadVerification();
                if (loadedFile) {
                    nrOfCores = (int)CPUcomboBox.SelectedValue;

                    // Lock the input fields
                    textBox_1a.IsEnabled = false;
                    textBox_1b.IsEnabled = false;
                    textBox_2a.IsEnabled = false;
                    textBox_2b.IsEnabled = false;
                    CPUcomboBox.IsEnabled = false;
                    CalculateButton.IsEnabled = false;
                    checkBox.IsEnabled = false;
                    // Unlock the Cancell button
                    cancelButton.IsEnabled = true;
                    
                    // Initialize some variables
                    progressIndicator.Clear();
                    progressIndicator.Add(startNumber);
                    progresslabel.Content = "0.00%";
                    newPrimes.Clear();

                    // Prepare the progresbar
                    // Reset the progressbar and asociated variables
                    progressBar.Minimum = startNumber;
                    progressBar.Maximum = endNumber;
                    progressBar.Value = startNumber;

                    // Writing out the initial values:
                    textBox1.Text = "";
                    textBox1.AppendText("Starting number: " + startNumber + "\r\n");
                    textBox1.AppendText("Ending number:   " + endNumber + "\r\n");
                    textBox1.AppendText("\r\n");
                    textBox1.AppendText("Number of cores used: " + nrOfCores + "\r\n");
                    textBox1.AppendText("\r\n");
                    textBox1.AppendText("Process started at: " + DateTime.Now.ToString("hh:mm:ss.ff") + "\r\n");
                    textBox1.AppendText("\r\n");


                    // Start the calculation taks initializer and workload separator method
                    mainTasker = new Thread(Tasker);
                    mainTasker.Start();
                }
            }
            
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e) {

            // Kill the threads
            for (int j = 0; j < nrOfCores; j++) {
                threads[j].Abort();
            }
            mainTasker.Abort();
            ub.Abort();
            

            // Unlock the input fields
            textBox_1a.IsEnabled = true;
            textBox_1b.IsEnabled = true;
            textBox_2a.IsEnabled = true;
            textBox_2b.IsEnabled = true;
            CPUcomboBox.IsEnabled = true;
            CalculateButton.IsEnabled = true;
            checkBox.IsEnabled = true;
            // Lock the Cancell button
            cancelButton.IsEnabled = false;

            textBox1.AppendText("Process aborted!\r\n");
        }

        private bool LoadVerification() {

            // Prepare the verifying prime numbers from the internal database

            Assembly _assembly;
            StreamReader _primeStreamReader;
            
            // Read the internal database
            try {
                _assembly = Assembly.GetExecutingAssembly();
                _primeStreamReader = new StreamReader(_assembly.GetManifestResourceStream("WPrime.Resources.Working_prime.txt"));
            }

            // Signal if the internal database cannot be read for some reason
            catch {
                MessageBox.Show("Error accessing resources!");
                return false;
            }

            // Get the internal primes out one by one and put it in a list
            using (_primeStreamReader) {
                string s = "";
                while ((s = _primeStreamReader.ReadLine()) != null) {
                    primelist.Add(uint.Parse(s));
                }
            }
            return true;
        }
        
        private bool ImputVerification() {

            // Check if the provided parameters are numbers
            try {
                a1 = decimal.Parse(textBox_1a.Text);
            }
            catch (FormatException){
                MessageBox.Show("You need to give a number for the first multiplier!");
                return false;
            }

            try {
                b1 = int.Parse(textBox_1b.Text);
            }
            catch (FormatException) {
                MessageBox.Show("You need to give a number for the first power!");
                return false;
            }

            try {
                a2 = decimal.Parse(textBox_2a.Text);
            }
            catch (FormatException) {
                MessageBox.Show("You need to give a number for the second multiplier!");
                return false;
            }

            try {
                b2 = int.Parse(textBox_2b.Text);
            }
            catch (FormatException) {
                MessageBox.Show("You need to give a number for the second power!");
                return false;
            }

            // Verify if both numbers are positive
            if (a1 < 0) {
                MessageBox.Show("The starting number must be positive!");
                return false;
            }
            if (a2 < 0) {
                MessageBox.Show("The ending number must be positive!");
                return false;
            }

            // Check if the numbers are not to big

            int a1Length = ((ulong)a1 == 0) ? 0:((ulong)a1).ToString().Length;
            int startNumberLength = a1Length + b1;

            if (startNumberLength > 17) {
                MessageBox.Show("The starting number is too big!");
                return false;
            }
            else if (startNumberLength == 17 && (ulong)a1 > 1) {
                MessageBox.Show("The starting number is too big!");
                return false;
            }

            int a2Length = ((ulong)a2 == 0) ? 0 : ((ulong)a2).ToString().Length;
            int endNumberLength = a2Length + b2;

            if (endNumberLength > 17) {
                MessageBox.Show("The ending number is too big!");
                return false;
            }
            else if (endNumberLength == 17 && (ulong)a2 > 1) {
                MessageBox.Show("The ending number is too big!");
                return false;
            }
            

            // Verify if the first number is smaller than the second
            startNumber = (ulong)(a1 * (decimal)Math.Pow(10, b1));
            endNumber = (ulong)(a2 * (decimal)Math.Pow(10, b2));

            if (startNumber < endNumber) {
                return true;
            }
            else return false;
        }

        private void Tasker() {
            
            finishedCores = 0;
            numberOfPrimesFound = 0;

            // Check if the starting variable is odd, if not increase it by 1
            if (startNumber % 2 == 0) {
                startNumber++;
            }
            
            // Strat up the stopwatch
            sw.Start();

            // Create the chosen nr of threads
            threads = new Thread[nrOfCores];

            // Start the updateBar thread
            ub = new Thread(UpdateBar);
            ub.Start();

            
            ulong numbersToCheck = endNumber - startNumber;

            // If all the numbers can be calculated in one go, do it
            if (numbersToCheck <= primeStorageLimiter) {

                // Assign the verification interval
                smallStartNumber = startNumber;
                smallEndNumber = endNumber;

                // Start up the calculator threads
                ThreadCreator();

                // Wait until all the threads are finished
                while (finishedCores != nrOfCores) {
                    Task.Delay(100);
                }

                // Stop the stopwatch, save the prime numbers and start the pos-processing
                sw.Stop();
                PrimeWriter();
                PostProcessing();
            }

            // else use the algorithm which partitions the job
            else {

                // Calculate the number of iteration sets necesarry to finish all calculations
                // Initialize logic and data variables
                int setsToDo = (int) Math.Ceiling((float)numbersToCheck / primeStorageLimiter);
                int finishedSets = 0;

                // Determine the first prime search interval
                smallStartNumber = startNumber;
                smallEndNumber = startNumber + primeStorageLimiter;

                // Start up the first set of threads
                ThreadCreator();

                // Start the set finish checking loop
                while (true) {

                    // If all the treads finished all the curent sets go for the next set
                    if (finishedCores == nrOfCores) {

                        // Reclculate the number of finished sets and reset the core finished counter
                        finishedSets++;
                        finishedCores = 0;

                        // If there is more sets to calculatate proceed
                        if (finishedSets < setsToDo) {

                            // Save the curent set of primes
                            PrimeWriter();

                            // Determine the new prime search interval
                            smallStartNumber = smallEndNumber;
                            smallEndNumber = ((endNumber - smallEndNumber) > primeStorageLimiter) ? smallEndNumber + primeStorageLimiter: endNumber;

                            // Start up the next set of threads
                            ThreadCreator();
                        }

                        // Else clean-up
                        else {

                            // Save the last set of primes and break out the check loop
                            PrimeWriter();
                            break;
                        }
                    }
                    Task.Delay(100);
                }

                // Stop the stopwatch and go for post-processing
                sw.Stop();
                PostProcessing();
            }
        }

        private void ThreadCreator() {
            for (int j = 0; j < nrOfCores; j++) {
                threads[j] = new Thread(PrimeVerifier);
                threads[j].Start(j);
            }
        }

        private void PrimeVerifier(object a) {

            // Initialize the local variables
            int threadNumber = (int) a;

            float modulus;

            ulong verifyUntilNumber;
            ulong loopstart = smallStartNumber + (ulong)(2 * threadNumber);
            ulong loopstep = (ulong) (2 * nrOfCores);

            // Loop through the numbers which needs to be checked for being prime numbers

            for (ulong i = loopstart ; i <= smallEndNumber; i += loopstep) {

                int dividerCounter = 0;

                // Calculate the number until the prospective prime number verification needs to go
                verifyUntilNumber = (ulong) Math.Ceiling(Math.Sqrt(i));

                // Divide the number until it's quare root, or until you find a divider (not checking for 1)
                for (int j = 0; primelist[j] <= verifyUntilNumber; j++) {
                    modulus = i % primelist[j];
                    if (modulus == 0) {
                        dividerCounter++;
                        if (dividerCounter > 0) {
                            break;
                        }
                    }
                }

                // Increase the progress
                lock (progressIndicator) {
                    progressIndicator[0] += 2;
                }

                // Check if the number of comon dividers is 0. If so, add it to the list of prime numbers
                if (dividerCounter == 0) {
                    lock (newPrimes) {
                        newPrimes.Add(i);
                    }
                }
            }

            // Signal that this thread finished all calculations
            finishedCores++;
        }

        private void PrimeWriter() {

            // Sort the primes in asscendic order
            newPrimes.Sort();

            // Make sure that the first prime is 2, not 1 (limitation of the algorithm)
            if (newPrimes[0] == 1)
                newPrimes[0] = 2;

            // Generate the new filename
            string filename = smallStartNumber + " - "+ smallEndNumber + ".txt";

            // Create a new file writer stream
            using (StreamWriter sw = new StreamWriter(Path.Combine(folderName, filename))) {
                foreach (ulong primes in newPrimes) {
                    sw.WriteLine(primes);
                }
            }

            // Indicate the save process
            Dispatcher.Invoke(() => { textBox1.AppendText(newPrimes.Count() + " primes were saved in: " + filename + "\r\n"); });
            Dispatcher.Invoke(() => { textBox1.AppendText("\r\n"); });

            // Store the number of primes found
            numberOfPrimesFound += (ulong)newPrimes.Count;

            // Empty the prime list variable
            newPrimes.Clear();
        }

        private void UpdateBar() {

            float percentage;
            
            do {
                // Calculate the percentage of finished job and update the % and progress bar
                percentage = (float)(progressIndicator[0] - startNumber) * 100 / (endNumber - startNumber);
                Dispatcher.Invoke(() => { progresslabel.Content = (percentage.ToString("0.00") + "%"); });
                Dispatcher.Invoke(() => { progressBar.Value = progressIndicator[0]; });
                Task.Delay(100);
            } while (progressIndicator[0] < endNumber);

            // Make sure to do the last round of updates
            Dispatcher.Invoke(() => { progresslabel.Content = ("100%"); });
            Dispatcher.Invoke(() => { progressBar.Value = progressIndicator[0]; });
        }

        private void PostProcessing() {

            // Write out the post-processing informations
            Dispatcher.Invoke(() => { textBox1.AppendText("Process finished at: " + DateTime.Now.ToString("hh:mm:ss.ff") + "\r\n"); });
            Dispatcher.Invoke(() => { textBox1.AppendText("\r\n"); });
            Dispatcher.Invoke(() => { textBox1.AppendText("No. of primes found: " + numberOfPrimesFound + "\r\n"); });
            Dispatcher.Invoke(() => { textBox1.AppendText("\r\n"); });
            Dispatcher.Invoke(() => { textBox1.AppendText("Time required to finish: " + sw.Elapsed.ToString() + "\r\n"); });

            // Reset the stopwach
            sw.Reset();

            // Clear the used prime list
            primelist.Clear();

            // Unlock the input fields
            Dispatcher.Invoke(() => { textBox_1a.IsEnabled = true; });
            Dispatcher.Invoke(() => { textBox_1b.IsEnabled = true; });
            Dispatcher.Invoke(() => { textBox_2a.IsEnabled = true; });
            Dispatcher.Invoke(() => { textBox_2b.IsEnabled = true; });
            Dispatcher.Invoke(() => { CPUcomboBox.IsEnabled = true; });
            Dispatcher.Invoke(() => { CalculateButton.IsEnabled = true; });
            Dispatcher.Invoke(() => { checkBox.IsEnabled = true; });
            // Lock the Cancell button
            Dispatcher.Invoke(() => { cancelButton.IsEnabled = false; });
        }
        
    }

}