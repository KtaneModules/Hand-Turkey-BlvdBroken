using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class handTurkey : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public Renderer[] feathers;
    public TextMesh colorblindText;
    public Color[] featherColors;
    public Color solveColor;
    
    public KMSelectable[] featherSelects;
    public KMSelectable beak;

    public AudioClip[] dishAudios;
    public AudioClip[] otherAudios;

    private int[] featherColorIndices = new int[4];
    private Position position;
    private int followed;

    private static readonly string[] fingerNames = new[] { "pinky", "ring", "middle", "index" };
    private static readonly string[] colorNames = new[] { "brown", "red", "orange", "yellow", "green", "blue", "pink" };
    private static readonly string[] dishNames = new[] { "turkey", "cranberry sauce", "pumpkin pie", "lemonade", "becherovka", "quahogs", "benadryl" };
    private static readonly int[] colorTable = new[] { 0, 3, 5, 3, 0, 4, 6, 4, 6, 4, 3, 0, 0, 1, 1, 4, 0, 5, 1, 5, 3, 1, 2, 1, 6, 2, 2, 3, 0, 2, 3, 1, 2, 0, 4, 5, 5, 2, 5, 2, 6, 4, 1, 5, 3, 6, 6, 4, 6 };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        Debug.LogFormat("Got to Awake");
        moduleId = moduleIdCounter++;
        for (int i = 0; i < 4; i++)
		{
            int j = i;
			KMSelectable feathe = featherSelects[j];
			featherSelects[j].OnInteract += delegate() { FeatherPress(feathe, j); return false; };
		}
        beak.OnInteract += delegate() { BeakPress(beak); return false; };
        //colorblindText.gameObject.SetActive(GetComponent<KMColorblindMode>().ColorblindModeActive);
    }

    private void FeatherPress(KMSelectable feath, int which)
    {
        if (!moduleSolved)
        {
            feath.AddInteractionPunch();

            if (colorTable[position.Current] == featherColorIndices[which])
            {
                audio.PlaySoundAtTransform(dishNames[featherColorIndices[which]], transform);
                GetComponent<KMBombModule>().HandlePass();
                moduleSolved = true;
                DebugOut("Module solved.");
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
                DebugOut(string.Format("Strike. {0} (feather color) is not at {1} (current position).", colorNames[featherColorIndices[which]], position.Coordinate()));
            }
        }
    }

    private void BeakPress(KMSelectable beakS)
    {
        if (!moduleSolved)
        {
            beakS.AddInteractionPunch();

            if (featherColorIndices.Contains(colorTable[position.Current]))
            {
                GetComponent<KMBombModule>().HandleStrike();
                DebugOut(string.Format("Strike. {0} (feather color) is at {1} (current position) and should have been pressed.", colorNames[colorTable[position.Current]], position.Coordinate()));
            }
            else
            {
                audio.PlaySoundAtTransform("other" + rnd.Range(0, 6), transform);
                GetComponent<KMBombModule>().HandlePass();
                moduleSolved = true;
                DebugOut("Module solved.");
            }
        }
    }

    private void Start()
    {
        colorblindText.text = "";
        for (int i = 0; i < 4; i++)
        {
            var color = rnd.Range(0, 7);
            featherColorIndices[i] = color;
            feathers[i].material.color = featherColors[color];
            DebugOut(string.Format("The feather on the {0} finger is {1}. You grabbed {2}.", fingerNames[i], colorNames[color], dishNames[color]));
            //colorblindText.text += "NROYGBP"[color];
        }

        position = new Position(FindStart(Voltage(), DateTime.Today));
        DebugOut(string.Format("Your starting position is {0}.", position.Coordinate()));

        // If you grabbed becherovka first...
        if (featherColorIndices[0] == 4)
        {
            DebugOut("You are an alcoholic. Move down 4 spaces.");
            DebugOut(position.ShiftRow(4));
            followed++;
        }
        // If you grabbed cranberry sauce, pumpkin pie, and lemonade...
        if (featherColorIndices.Contains(1) && featherColorIndices.Contains(2) && featherColorIndices.Contains(3)) 
        {
            DebugOut("You are on a sugar high. Move up 3 spaces.");
            DebugOut(position.ShiftRow(-3));
            followed++;
        }
        // If you grabbed something more than once...
        for (int i = 0; i < 3; i++)
        {
            if (featherColorIndices.Count(x => x == featherColorIndices[i]) > 1)
            {
                int fod = featherColorIndices[i];
                int lowest = 8;
                int direction = 4;
                Position temPos = new Position(position.Current);
                int dist = 0;
                bool found = false;
                while(!found)
                {
                    temPos.ShiftCol(-1);
                    dist++;
                    // if gone over edge, stop
                    if (temPos.Col == 6)
                    {
                        found = true;
                    }
                    // else if found next orthagonal, set direction and save dist and stop
                    else if ((colorTable[temPos.Current] == fod) && (dist < lowest))
                    {
                        lowest = dist;
                        direction = 0;
                        found = true;
                    }
                }
                // go back to initial col
                temPos.ShiftCol(dist);
                // this really could have been a function
                dist = 0;
                found = false;
                while(!found)
                {
                    temPos.ShiftRow(-1);
                    dist++;
                    if (temPos.Row == 6)
                    {
                        found = true;
                    }
                    else if ((colorTable[temPos.Current] == fod) && (dist < lowest))
                    {
                        lowest = dist;
                        direction = 1;
                        found = true;
                    }
                }
                temPos.ShiftRow(dist);
                dist = 0;
                found = false;
                while(!found)
                {
                    temPos.ShiftCol(1);
                    dist++;
                    if (temPos.Col == 0)
                    {
                        found = true;
                    }
                    else if ((colorTable[temPos.Current] == fod) && (dist < lowest))
                    {
                        lowest = dist;
                        direction = 2;
                        found = true;
                    }
                }
                temPos.ShiftCol(-1 * dist);
                dist = 0;
                found = false;
                while(!found)
                {
                    temPos.ShiftRow(1);
                    dist++;
                    if (temPos.Row == 0)
                    {
                        found = true;
                    }
                    else if ((colorTable[temPos.Current] == fod) && (dist < lowest))
                    {
                        lowest = dist;
                        direction = 3;
                        found = true;
                    }
                }
                

                if (direction < 4)
                {
                    DebugOut(string.Format("You took {0} more than once. Move {1} 2 spaces.", dishNames[fod], new[] { "left", "up", "right", "down" }[direction]));
                }
                else
                {
                    DebugOut(string.Format("You thought you took {0} more than once, but it was simply a dream.", dishNames[fod]));
                }

                if (direction == 0)
                {
                    DebugOut(position.ShiftCol(-2));
                }
                else if (direction == 1)
                {
                    DebugOut(position.ShiftRow(-2));
                }
                else if (direction == 2)
                {
                    DebugOut(position.ShiftCol(2));
                }
                else if (direction == 3)
                {
                    DebugOut(position.ShiftRow(2));
                }
                followed++;
                // breaks for loop after leftmost followed
                i = 3;
            }
        } 
        // If you grabbed four different items...
        if (featherColorIndices.Distinct().Count() == 4)
        {
            DateTime tempDate = DateTime.Today;
            tempDate = tempDate.AddYears(((tempDate.Month < 3) ? -1 : 0));
            while(!(DateTime.IsLeapYear(tempDate.Year)))
            {
                tempDate = tempDate.AddYears(-1);
            }
            DateTime lastleap = new DateTime(tempDate.Year, 2, 29);
            TimeSpan funnt = DateTime.Today.Subtract(lastleap);
            Debug.LogFormat("days since last leap: {0}", funnt.Days);
            DateTime lastThanksgiving = new DateTime((DateTime.Today.Year - 1), 11, 22);
            while(lastThanksgiving.DayOfWeek != DayOfWeek.Thursday)
            {
                lastThanksgiving = lastThanksgiving.AddDays(1);
            }
            Debug.LogFormat("last thanks was: {0}", lastThanksgiving.ToString("MMddyyyy"));
            var opp = new Position(FindStart(funnt.Days % 10, lastThanksgiving, false));
            int tylerninjablevins = Math.Abs(position.Row - opp.Row) + Math.Abs(position.Col - opp.Col);
            DebugOut(string.Format("You are feeling festive. Move {0} space(s) in reading order.", tylerninjablevins));
            DebugOut(position.MoveReading(tylerninjablevins));
            followed++;
        }
        // If you grabbed turkey or cranberry sauce last...
        if (featherColorIndices[3] < 2)
        {
            int lefts = 0;
            for (int i = position.Col; i < 49; i += 7)
            {
                if ((colorTable[i] < 3) || (colorTable[i] == 5))
                {
                    lefts++;
                }
            }
            DebugOut(string.Format("You are taking home leftovers. Move {0} space(s) left.", lefts));
            DebugOut(position.ShiftCol(-1 * lefts));
            followed++;
        }
        // If you grabbed quahogs and benadryl...
        if (featherColorIndices.Contains(5) && featherColorIndices.Contains(6))
        {
            DebugOut("You are allergic to shellfish. Divide your current row and column by 2, rounding down, and add 2 to each.");
            position.ShiftCol((position.Col / -2) + 2);
            DebugOut(position.ShiftRow((position.Row / -2) + 2));
            followed++;
        }
        // If you did not have any cranberry sauce, lemonade, or becherovka...
        if (!featherColorIndices.Contains(1) && !featherColorIndices.Contains(3) && !featherColorIndices.Contains(4))
        {
            int cornerChoice = ((position.Col > 3) ? 1 : 0) + ((position.Row > 3) ? 2 : 0);
            String[] corners = { "top left", "top right", "bottom left", "bottom right" };
            DebugOut(string.Format("You have dry lips. Move opposite to the {0} corner.", corners[cornerChoice]));
            var closestCorner = new Position(((position.Col > 3) ? 6 : 0) + ((position.Row > 3) ? 42 : 0));
            position.ShiftCol(position.Col - closestCorner.Col);
            DebugOut(position.ShiftRow(position.Row - closestCorner.Row));
            followed++;
        }
        // If you grabbed three or more total of turkey or pumpkin pie...
        if (featherColorIndices.Count(f => (f == 0) || (f == 2)) >= 3)
        {
            int moved = 1;
            for (int i = 0; i < (bomb.GetBatteryHolderCount() + bomb.GetIndicators().Count()); i++)
            { // this very well might just be advanced multiplication, but it helps me understand more
                moved += (bomb.GetPortCount() + 1);
            }
            DebugOut(string.Format("You just came back from a “walk”. Make the knight's move that is {0} from north.", moved));
            int[] kma = new[] { -2, 1 }; int[] kmb = new[] { -1, 2 }; int[] kmc = new[] { 1, 2 }; int[] kmd = new[] { 2, 1 }; int[] kme = new[] { 2, -1 }; int[] kmf = new[] { 1, -2 }; int[] kmg = new[] { -1, -2 }; int[] kmh = new[] { -2, -1 };
            int[][] knightMoves = new int[][] { kma, kmb, kmc, kmd, kme, kmf, kmg, kmh };
            knightMoves = knightMoves.Where(x => ((position.Row + x[0] >= 0) && (position.Row + x[0] <= 6) && (position.Col + x[1] >= 0) && (position.Col + x[1] <= 6))).ToArray();
            int[] toMake = knightMoves[((moved - 1) % knightMoves.Count())];
            position.ShiftRow(toMake[0]);
            DebugOut(position.ShiftCol(toMake[1]));
            followed++;
        }
        // If you had three or more benadryls...
        if (featherColorIndices.Count(f => (f == 6)) >= 3)
        {
            DebugOut("NIGHTMARE NIGHTMARE NIGHTMARE\nMove left, northwest, southwest, right, up, right, left, down, and finally move to the position diametrically opposite of the position you end up in.");
            DebugOut(position.ShiftCol(-1)); // left
            position.ShiftCol(-1); // northwest
            DebugOut(position.ShiftRow(-1));
            position.ShiftCol(-1); // southwest
            DebugOut(position.ShiftRow(1));
            DebugOut(position.ShiftCol(1)); // right
            DebugOut(position.ShiftRow(-1)); // up
            DebugOut(position.ShiftCol(1)); // right
            DebugOut(position.ShiftCol(-1)); // left
            DebugOut(position.ShiftRow(1)); // down
            position.Current = 48 - position.Current;
            position.Row = 6 - position.Row;
            position.Col = 6 - position.Col;
            DebugOut(position.ShiftRow(0));
            // TODO: Correct code
            //DebugOut(position.MoveReading(1 + 2 * (48 - position.Current))); // opposite
            followed++;
        }
        int move = 0;
        for (int i = 0; i < (10 - followed); i++)
        {
            Debug.LogFormat(position.Current.ToString());
            if (featherColorIndices.Contains(colorTable[position.Current]))
            {
                for (int j = 3; j > -1; j--)
                {
                    if (featherColorIndices[j] == colorTable[position.Current])
                    {
                        move = 1 - 2 * (j % 2);
                    }
                }
                DebugOut(position.ShiftCol(move));
            }
            else
            {
                DebugOut(position.ShiftRow(-1));
            }
        }
    }

    private int FindStart(double voltage, DateTime today, bool write = true)
    {
        var startingRow = 0;
        var startingColumn = 0;
        if (voltage != -1d)
        {
            startingRow = (int)(Math.Floor(voltage) % 7);
            if (write) DebugOut(string.Format("A voltage meter is present. The starting row is {0}.", (startingRow + 1)));
        }
        else
        {
            startingRow = (colorNames[featherColorIndices[1]].Length + dishNames[featherColorIndices[3]].Replace(" ", "").Length) % 7;
            if (write) DebugOut(string.Format("A voltage meter is not present. The number of letters in {0} plus the number of letters in {1} modulo 7, plus one is {2}. This is the starting row.", colorNames[featherColorIndices[1]], dishNames[featherColorIndices[3]], (startingRow + 1)));
        }
        var date = today.ToString("MMddyyyy");
        startingColumn = int.Parse(date) % 7;
        if (write) DebugOut(string.Format("The concatenated date is {0}, modulo 7, plus one is {1}. This is the starting column.", date, (startingColumn + 1)));
        int SPosition = startingRow * 7 + startingColumn;
        return SPosition;
    }

    private double Voltage()
    {
        if (bomb.QueryWidgets("volt", "").Count() != 0)
        {
            double TempVoltage = double.Parse(bomb.QueryWidgets("volt", "")[0].Substring(12).Replace("\"}", ""));
            return TempVoltage;
        }
        return -1d;
    }

    private void DebugOut(string inS)
    {
        Debug.LogFormat("[Hand Turkey #{0}] {1}", moduleId, inS);
    }

    private class Position
    {
        public int Current { get; set; }
        public int Col { get; set; }
        public int Row { get; set; }

        public Position(int position)
        {
            this.Current = position;
            this.Col = position % 7;
            this.Row = position / 7;
        }

        public string ShiftCol(int amt)
        {
            this.Col = this.Modulo((this.Col + amt), 7);
            this.Current = this.Row * 7 + this.Col;
            return string.Format("You are now at {0}.", this.Coordinate());
        }

        public string ShiftRow(int amt)
        {
            this.Row = this.Modulo((this.Row + amt), 7);
            this.Current = this.Row * 7 + this.Col;
            return string.Format("You are now at {0}.", this.Coordinate());
        }

        public string MoveReading(int amt)
        {
            this.Current = this.Modulo((this.Current + amt), 49);
            this.Col = this.Current % 7;
            this.Row = this.Current / 7;
            return string.Format("You are now at {0}.", this.Coordinate());
        }

        public string Coordinate()
        {
            return ("ABCDEFG"[this.Col].ToString() + (this.Row + 1).ToString());
        }

        // i hate c sharp i hate c sharp i hate c sharp i hate c sharp i hate c sharp i hate c sharp
        public int Modulo(int x, int m)
        {
            return (x % m + m) % m;
        }
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} press <1-4> [Presses the specified finger in standard finger counting order] | !{0} mouth [Presses the mouth]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string input)
    {
        input = input.Trim().ToLowerInvariant();
        switch (input)
        {
            case "mouth":
                yield return null;
                beak.OnInteract();
                break;
            case "press 1":
                yield return null;
                featherSelects[3].OnInteract();
                break;
            case "press 2":
                yield return null;
                featherSelects[2].OnInteract();
                break;
            case "press 3":
                yield return null;
                featherSelects[1].OnInteract();
                break;
            case "press 4":
                yield return null;
                featherSelects[0].OnInteract();
                break;
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = 0; i < featherColorIndices.Length; i++)
        {
            if (colorTable[position.Current] == featherColorIndices[i])
            {
                featherSelects[i].OnInteract();
                yield return new WaitForSeconds(.1f);
                yield break;
            }
        }
        beak.OnInteract();
        yield return new WaitForSeconds(.1f);
    }
}
