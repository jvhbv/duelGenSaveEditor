using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace duelGenerationsSaveEditor
{
    public partial class MainWindow : Window
    {
        public List<int> cardIDs = new List<int>(); //stores the IDs of each card
        public List<string> cards = new List<string>(); //stores the names of each card
        public int[] amounts; //stores the amount of each card you have, an array rather than a list to easily initialize it without having to add each item individually
        public List<byte> bytes = new List<byte>(); //stores the entire loaded file into memory as a list of bytes

        public byte[] GetAllBytes(string file)
        {
            return File.ReadAllBytes(file);
        }

        public MainWindow()
        {
            InitializeComponent();

            var directory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var file = System.IO.Path.Combine(directory, "ygoIDs.tsv"); //all of the IDs and everything are pulled straight off the Wikia
            using (var reader = new StreamReader(file))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split('\t');

                    int IDs;
                    bool isNumber = int.TryParse(values[0], out IDs);
                    if (isNumber)
                    {
                        cardIDs.Add(IDs);
                        cards.Add(values[1]);
                    }
                }
                foreach (string mons in cards)
                {
                    CardNames.Items.Add(mons);
                }
            }
            amounts = new int[cardIDs.Count];
        }

        public void HandleCardLib()
        {
            //I want to skip the first 4 bytes of the file, then parse the next 2 bytes as a single integer, then parse the next byte as an int, then repeat
            int repeated = 1; //starting with repeated at 1 eliminates the need to remove the first 4 bytes from the list
            while (repeated < bytes.Count / 4)
            {
                int cardID = BitConverter.ToUInt16(bytes.ToArray(), repeated * 4);
                int amount = bytes[repeated * 4 + 2];
                //get cardIDs from initial list and then use the amounts list to store the amounts at the same index
                int indexOfID = cardIDs.FindIndex(x => x == cardID);
                amounts[indexOfID] = amount;
                repeated++;
            }
        }

        private void open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.DefaultExt = ".dat";
            openFile.Filter = "Card Library (*.dat)|*.dat";
            Nullable<bool> result = openFile.ShowDialog();

            if (result == true)
            {
                bytes = GetAllBytes(openFile.FileName).ToList<byte>();
                HandleCardLib();
            }
        }

        private void CardNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (amounts != null) //prevents this from being ran before you load a card library
            { 
                number.Text = amounts[CardNames.SelectedIndex].ToString(); 
            }
        }

        private void giveAllCards_Click(object sender, RoutedEventArgs e)
        {
            //I guess the easiest way to go about this is to first find all the cards you don't already have, then add them into the list, then set all of the number owned to 255
            //The only problem is the 1 unknown byte at the end of each card. I'll try first setting that to 0x00, and if that doesn't work I'll look more into it
            int repeated = 1;
            List<int> cardID = new List<int>();
            while (repeated < bytes.Count / 4)
            {
                cardID.Add(BitConverter.ToUInt16(bytes.ToArray(), repeated * 4)); //this puts every card you already have into 1 list, now I need a way to find all the ones you don't have
                bytes[repeated * 4 + 2] = 250; //sets all of the cards you already have to owning 255 of them, so another loop doesn't have to be ran later
                repeated++;
            }
            foreach(int card in cardIDs)
            {
                if (!cardID.Contains(card))
                {
                    List<byte> test = BitConverter.GetBytes(card).ToList<byte>();
                    test.RemoveRange(2, 2);
                    test.Add(250);
                    test.Add(0);
                    try //basically, this try catch function is just pretty much doing 1 thing if you have a card that comes after the one it is checking for, and another if you don't
                    {
                        int index = cardID.FindIndex(x => x > card); //finds the first card you have that comes after the card in question
                        cardID.Insert(index, card);
                        bytes.Insert(index * 4 + 4, test[0]); //I'm having this manually inserted for now, but I'll probably change this to a loop at some point
                        bytes.Insert(index * 4 + 5, test[1]);
                        bytes.Insert(index * 4 + 6, test[2]);
                        bytes.Insert(index * 4 + 7, test[3]);
                    }
                    catch
                    {
                        cardID.Add(card);
                        bytes.Add(test[0]); //I'm having this manually inserted for now, but I'll probably change this to a loop at some point
                        bytes.Add(test[1]);
                        bytes.Add(test[2]);
                        bytes.Add(test[3]);
                    }
                }
            }
            int totalcards = 0;
            for (int i = 0; i < cardIDs.Count; i++)
            {
                amounts[i] = 250;
                totalcards++;
            }
            bytes[2] = BitConverter.GetBytes(totalcards)[0];
            bytes[3] = BitConverter.GetBytes(totalcards)[1];
        }

        private void export_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.DefaultExt = ".dat";
            openFile.Filter = "Card Library (*.dat)|*.dat";
            Nullable<bool> result = openFile.ShowDialog();

            if (result == true)
            {
                File.WriteAllBytes(openFile.FileName, bytes.ToArray());
            }
        }
    }
}
