using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using Microsoft.VisualBasic;

namespace Consult
{
    public class ECU
    {
        private static byte[] ECU_INIT = { 0xff, 0xff, 0xef, (byte)'\0' };
        private static byte[] ECU_STREAM_START = { 0xF0, (byte)'\0' };
        private static byte[] ECU_STREAM_STOP = { 0x30, (byte)'\0' };
        private static byte[] ECU_ERROR_RESPONSE = { 0xFE, (byte)'\0' };
        private static byte[] ECU_VALID_RESPONSE = { 0xFF, (byte)'\0' };
        private static byte[] ECU_HELLO = { 0x10, (byte)'\0' };

        /* ECU Sensors */
        private static byte[] READ_RPM_MSB = { 0X5A, 0X00, (byte)'\0' };
        private static byte[] READ_RPM_LSB = { 0X5A, 0X01, (byte)'\0' };
        private static byte[] READ_MAF_MSB = { 0x5a, 0x04, (byte)'\0' };
        private static byte[] READ_MAF_LSB = { 0x5a, 0x05, (byte)'\0' };
        private static byte[] READ_COOLANT_TEMP = { 0X5A, 0X08, (byte)'\0' };
        private static byte[] READ_LHO2 = { 0x5a, 0x09, (byte)'\0' };
        private static byte[] READ_RHO2 = { 0x5a, 0x0a, (byte)'\0' };
        private static byte[] READ_KMH = { 0X5A, 0X0B, (byte)'\0' };
        private static byte[] READ_BATTERY_VOLTAGE = { 0X5A, 0X0C, (byte)'\0' };
        private static byte[] READ_THROTTLE_POSITION = { 0X5A, 0X0D, (byte)'\0' };
        private static byte[] READ_FUEL_TEMPERATURE = { 0X5A, 0X0F, (byte)'\0' };
        private static byte[] READ_INTAKE_TEMPERATURE = { 0X5A, 0X11, (byte)'\0' };
        private static byte[] READ_EXHAUST_TEMPERATURE = { 0X5A, 0X12, (byte)'\0' };
        private static byte[] READ_INJECTION_TIMING_LH_MSB = { 0x5A, 0x14, (byte)'\0' };
        private static byte[] READ_INJECTION_TIMING_LH_LSB = { 0x5A, 0x15, (byte)'\0' };
        private static byte[] READ_IGNITION_TIMING = { 0x5A, 0x16, (byte)'\0' };
        private static byte[] READ_AAC_VALVE = { 0x5A, 0x17, (byte)'\0' };
        private static byte[] READ_AF_ALPHA_LH = { 0x5A, 0x1A, (byte)'\0' };
        private static byte[] READ_AF_ALPHA_RH = { 0x5A, 0x1B, (byte)'\0' };
        private static byte[] READ_INJECTION_TIMING_RH_MSB = { 0x5A, 0x22, (byte)'\0' };
        private static byte[] READ_INJECTION_TIMING_RH_LSB = { 0x5A, 0x23, (byte)'\0' };
        private static byte[] READ_WASTE_GATE_SOLENOID = { 0x5A, 0x28, (byte)'\0' };
        private static byte[] READ_FUEL_GAUGE = { 0x5A, 0x2F, (byte)'\0' };

        private static byte[] READ_FAULTS = { 0xD1, (byte)'\0' };

        /* Digital Bit Table */
        private static byte[] READ_DIGITAL_TABLE_1 = { 0x5A, 0x13, (byte)'\0' };
        private static byte[] READ_DIGITAL_TABLE_2 = { 0x5A, 0x1e, (byte)'\0' };
        private static byte[] READ_DIGITAL_TABLE_3 = { 0x5A, 0x1f, (byte)'\0' };
        private static byte[] READ_DIGITAL_TABLE_4 = { 0x5A, 0x21, (byte)'\0' };

        private static int DBT_ac_on = 5;
        private static int DBT_power_steering = 4;
        private static int DBT_park_neutral = 3;
        private static int DBT_start_signal = 2;
        private static int DBT_closed_throttle_position = 1;

        private static int DBT_ac_relay = 8;
        private static int DBT_fuel_pump_relay = 7;
        private static int DBT_valve_timing_solenoid = 6;
        private static int DBT_coolant_fan_hi = 2;
        private static int DBT_coolant_fan_lo = 1;

        private static int DBT_pressure_relief_valve = 7;
        private static int DBT_wastegate_solenoid = 6;
        private static int DBT_idle_air_control_valve = 4;
        private static int DBT_egr_solenoid = 1;

        private static int DBT_lh_bank_lean = 8;
        private static int DBT_rh_bank_lean = 7;

        private static byte[] HALT_ACTIVE_TEST = { 0xF0, (byte)'\0' };
        private static byte[] TEST_FUEL_INJECTION = { 0x0A, 0x80, (byte)'\0' };
        private static byte[] TEST_FUEL_INJECTION_VALUES = { 0x5a, 0x5b, 0x5c, 0x5d, 0x5e, 0x5f, 0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e };

        private static byte[] TEST_IGNITION_TIMING = { 0x0a, 0x80, (byte)'\0' };
        private static byte[] TEST_IGNITION_TIMING_VALUES = { 0xFB, 0xFC, 0xfd, 0xfe, 0xff, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };

        private bool _ECUonline = false;
        //private termios options = new termios();
        private int _SerialPortState = 0;
        private int _Comms_flush_error = 0;
        private string _PortName = "COM1";
        private System.IO.Ports.SerialPort _SerialPort; // = new System.IO.Ports.SerialPort(_PortName);

        /* check if the ECU has been initialised */
        private bool ECUonline()
        {
            return _ECUonline;
        }

        /* initialises the port that consult is connected to and makes contact with the ECU */
        private int ECUinit()
        {
            int ResponseByteCounter = 0;
            COMMSresponse response = new COMMSresponse();

            // If the ECU is already started, do nothing
            if (ECUonline())
                return 1;

            // If the comms have been started, do nothing, else init it
            if (!COMMSonline())
            {
                COMMSinit();
                if (!COMMSonline())
                {
                    Debug.Print("Cannot start comms, THIS IS NO GOOD\n");
                    return 0;
                }
            }

            // Initialise the ECU
            // It is important to actually try twice, as the first time it may ignore the init command

            COMMSissueCommand(ECU_INIT, 3);
            if (COMMSreadResponse(response) != true)
            {
                Debug.Print("ECU_INIT: Timeout, Trying again..\n");

                COMMSissueCommand(ECU_INIT, 3);
                if (COMMSreadResponse(response) != true)
                {
                    Debug.Print("ECU_INIT: Timeout, CONSULT may not be connected\n");
                    return 0;
                }
            }

            // Check the response. Did we get a HELLO from the onboard?

            //if (StringFunctions.GetOffsetOfArrayInArray(response.data, 0, 0, ECU_HELLO) != null)
            if (response.data.Contains(ECU_HELLO))
            {
                Debug.Print("ECU_INIT: Hello From Nissan\n");
            }
            else
            {
                Debug.Print("ECU_INIT: ECU responds with garbage.\n");
                for (ResponseByteCounter = 0; ResponseByteCounter < 10; ResponseByteCounter++)
                    Debug.Print("0x%2X, ", response.data[ResponseByteCounter]);
                return 0;
            }


            // ECU is online and listening for commands
            _ECUonline = true;

            return 1;
        }

        /* Using TWO commands to the ECU at once, this way we read the values
         * and form an int. is the responsibility of the user to determine what
         * needs to be done with this var
         * Return 0 or value = success
         * Return -1 mean failure
        */
        private int ECUgetTwoRegisters(byte[] command_msb, byte[] command_lsb)
        {
            int pos = 0;
            int value = 0;
            COMMSresponse response = new COMMSresponse();

            // Check that the ECU is ready
            if (!ECUonline())
                return -1;

            // Get MSB and LSB registers
            if (COMMSissueCommand(command_msb, 2) == false)
                return -1;
            if (COMMSissueCommand(command_lsb, 2) == false)
                return -1;

            if (COMMSreadResponse(response) == false)
                return -1;

            // Check command and response
            // FIX
            // We want to search through the data till we get a A5 for reading registers

            // Start stream to get data in comm buffer
            if (!COMMSstartStream())
                return -1;
            if (!COMMSreadResponse(response))
                return -1;

            for (pos = 0; pos < response.len; pos++)
                if (response.data[pos] == ECU_VALID_RESPONSE[0] || response.data[pos] == ECU_ERROR_RESPONSE[0])
                    break;

            if (response.data[pos] == ECU_ERROR_RESPONSE[0])
            {
                // This function used to alwasy return -1; however.
                // cos the stream is so long we will pick up the right value.
            }

            // Push and OR MSB and LSB
            value = response.data[pos + 2]; // MSB
            value <<= 8; // Shift the MSB left
            value |= response.data[pos + 3]; // LSB

            // Stop stream and flush comms buffer.
            COMMSstopStream();
            COMMSflush();

            return value;
        }

        /* ask for a single value from the ECU. it is up to the user to
 * determine what this value is and what needs to be done with it.
 * Return 0 or pos integer is success
 * Return -1 is failure
 */
        private int ECUgetRegister(byte[] command)
        {
            int pos = 0;
            int value = 0;
            COMMSresponse response = new COMMSresponse();

            // Check that the ECU is ready
            if (!ECUonline())
                return -1;

            // Read a single register
            if (COMMSissueCommand(command, 2) == false)
                return -1;
            if (COMMSreadResponse(response) == false)
                return -1;

            // Check command and response
            // FIX
            // We want to search through the data till we get a A5 for reading registers

            // Start stream to get data in comm buffer
            if (!COMMSstartStream())
                return -1;
            if (!COMMSreadResponse(response))
                return -1;

            for (pos = 0; pos < response.len; pos++)
                if (response.data[pos] == ECU_VALID_RESPONSE[0] || response.data[pos] == ECU_ERROR_RESPONSE[0])
                    break;

            // Check that it was acutally found.
            if (response.data[pos] == ECU_ERROR_RESPONSE[0])
            {
                // This function used to alwasy return -1; however.
                // cos the stream is so long we will pick up the right value.
            }

            // set value
            value = (int)response.data[pos + 2];

            // Stop stream and flush comms buffer.
            COMMSstopStream();
            COMMSflush();

            return value;


        }

        /* initialises the port that consult is connected to 
         * 1 = success
         * 0 = cannot init serial port
         */
        private int COMMSinit()
        {
            //int spin = 0;
            _SerialPort = new System.IO.Ports.SerialPort(_PortName);
            _SerialPort.BaudRate = 9600;
            _SerialPort.DataBits = 8;
            _SerialPort.Parity = Parity.None;
            _SerialPort.ReadBufferSize = 1000;

            try
            {
                _SerialPort.Open();

                _SerialPortState = 1;
                /*fd = open(CONSULT_PORT, O_RDWR | O_NDELAY);

                if (fd == -1)
                {
                    perror("open");
                    return 0;
                }*/

                // Get current options
                //tcgetattr(fd, options);

                // Reset port
                //cfmakeraw(options);

                // Set the baud rates to 9600...
                //cfsetispeed(options, B9600);
                //cfsetospeed(options, B9600);


                // Set character size to 8 bits
                //options.c_cflag &= ~CSIZE; // Mask the character size bits
                //options.c_cflag |= CS8; // Select 8 data bits

                // Set the new options for the port...
                /*if (tcsetattr(fd, TCSANOW, options) != 0)
                {
                    perror("tcsetattr");
                    return 0;
                }*/

                // Comms started!
            }
            catch (Exception)
            {
                return 0;
            }

            return 1;

        }

        /* Have the comms been setup by another function, maybe HICAS init? */
        private bool COMMSonline()
        {
            return (_SerialPortState > 0) ? true : false;
        }

        /* Write a command to the comms port 
         * <= 0 = Failure of comms
         * >  0 = Bytes sent
         */
        private bool COMMSissueCommand(byte[] command, int len)
        {
            // Check the comms are working
            if (COMMSonline() == false)
                return false;

            try
            {
                _SerialPort.Write(command, 0, len);
                //return (write(_SerialPortState, command, len));
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        /* Get the response from the comms
         * 1 = Success
         * 0 = failure
         */
        private bool COMMSreadResponse(COMMSresponse response)
        {
            byte[] buffer = { (byte)'\0' };
            string StringBuffer = "";
            int spin = 0;
            int ResultOfRead = 0;
            int Counter = 0;

            if (response == null)
                return false;

            if (COMMSonline() == false)
                return false;

            response = null;
            response.len = 0;

            // Read for a while, but not too long.
            while (StringBuffer.Length == 0 && spin <= 200)
            {
                try
                {
                    StringBuffer = _SerialPort.ReadExisting();

                    spin++;
                    System.Threading.Thread.Sleep(10);
                }
                catch (Exception)
                {
                    return false;
                    /*
                     * // Did read succeed?
                     if (ResultOfRead <= 0)
                     {
                         //fprintf( stderr, "COMMSreadResponse: read timeout\n" );
                         return false;
                     }
                    */
                }

            }

            response.data = StringBuffer;


            // Move through response and find where it started, OBC often pads NULLS at the start.
            for (Counter = 0; Counter <= ResultOfRead; Counter++)
                if (buffer[Counter] != null)
                    break;

            if (Counter == ResultOfRead)
            {
                //fprintf( stderr,"COMMSreadResponse: read 1000 NULLS?\n" );
                return false;
            }

            //C++ TO C# CONVERTER TODO TASK: The memory management function 'memcpy' has no equivalent in C#:
            memcpy(response.data, buffer[Counter], ResultOfRead - Counter);
            response.len = ResultOfRead - Counter;

            return true;
        }

        /* Start streaming data from the ECU */
        private bool COMMSstartStream()
        {
            return write(_SerialPortState, ECU_STREAM_START, 1);
        }

        /* Stop the streaming data */
        private bool COMMSstopStream()
        {
            return write(_SerialPortState, ECU_STREAM_STOP, 1);
        }

        private byte COMMSreadResult()
        {
            int spin = 0;
            int a = 0;
            int ResultOfRead = 0;
            byte[] buffer = { (byte)'\0' };

            while ((ResultOfRead = read(_SerialPortState, buffer, 1000)) <= 0 && spin <= 100)
            {
                spin++;
                System.Threading.Thread.Sleep(10);
            }

            if (ResultOfRead <= 0)
                return 0;

            for (a = 0; a <= 100; a++)
                if (buffer[a] == 0xFF)
                    break;

            write(_SerialPortState, ECU_STREAM_STOP, 1);
            return (byte)buffer[a];
        }

        /* Read all the data out that has been placed on the buffer. 
         * We should really look for 0xCF which means end of stream. 
         */
        private void COMMSflush()
        {
            int spin = 0;
            int ResultOfRead = 0;
            int end = 0;
            byte[] buffer = new byte[1000];

            while ((ResultOfRead = read(_SerialPortState, buffer, 1000)) <= 0 && spin <= 10)
            {
                spin++;
                System.Threading.Thread.Sleep(10);
            }

            for (ResultOfRead = 0; ResultOfRead <= 1000; ResultOfRead++)
                if (buffer[ResultOfRead] == 0xCF)
                {
                    end = 1;
                    break;
                }

            // If we didnt see end of stream, try again, but if we do this 
            // more then 5 times just finish
            if (end == 0 && _Comms_flush_error < 5)
            {
                _Comms_flush_error++;
                COMMSflush();
            }
            else
            {
                _Comms_flush_error = 0;
            }
        }
    }

    //----------------------------------------------------------------------------------------
    //	Copyright © 2006 - 2012 Tangible Software Solutions Inc.
    //	This class can be used by anyone provided that the copyright notice remains intact.
    //
    //	This class provides the ability to simulate various classic C string functions
    //	which don't have exact equivalents in the .NET Framework.
    //----------------------------------------------------------------------------------------
    internal static class StringFunctions
    {
        private static string activestring;
        private static int activeposition;

        //------------------------------------------------------------------------------------
        //	This method allows replacing a single character in a string, to help convert
        //	C++ code where a single character in a character array is replaced.
        //------------------------------------------------------------------------------------
        internal static string ChangeCharacter(string sourcestring, int charindex, char changechar)
        {
            return (charindex > 0 ? sourcestring.Substring(0, charindex) : "")
                + changechar.ToString() + (charindex < sourcestring.Length - 1 ? sourcestring.Substring(charindex + 1) : "");
        }

        //------------------------------------------------------------------------------------
        //	This method simulates the classic C string function 'isxdigit' (and 'iswxdigit').
        //------------------------------------------------------------------------------------
        internal static bool IsXDigit(char character)
        {
            if (char.IsDigit(character))
                return true;
            else if ("ABCDEFabcdef".IndexOf(character) > -1)
                return true;
            else
                return false;
        }

        //------------------------------------------------------------------------------------
        //	This method simulates the classic C string function 'strchr' (and 'wcschr').
        //------------------------------------------------------------------------------------
        internal static string StrChr(string stringtosearch, char chartofind)
        {
            int index = stringtosearch.IndexOf(chartofind);
            if (index > -1)
                return stringtosearch.Substring(index);
            else
                return null;
        }

        //------------------------------------------------------------------------------------
        //	This method simulates the classic C string function 'strrchr' (and 'wcsrchr').
        //------------------------------------------------------------------------------------
        internal static string StrRChr(string stringtosearch, char chartofind)
        {
            int index = stringtosearch.LastIndexOf(chartofind);
            if (index > -1)
                return stringtosearch.Substring(index);
            else
                return null;
        }

        //------------------------------------------------------------------------------------
        //	This method simulates the classic C string function 'strstr' (and 'wcsstr').
        //------------------------------------------------------------------------------------
        internal static string StrStr(string stringtosearch, string stringtofind)
        {
            int index = stringtosearch.IndexOf(stringtofind);
            if (index > -1)
                return stringtosearch.Substring(index);
            else
                return null;
        }

        public static int GetOffsetOfArrayInArray(byte[] bigArray, int bigArrayOffset, int bigArrayCount, byte[] smallArray)
        {
            // TODO: Check whether none of the variables are null or out of range. 
            if (smallArray.Length == 0)
                return 0;

            List<int> starts = new List<int>();    // Limited number of elements. 

            int offset = bigArrayOffset;
            // A single pass through the big array. 
            while (offset < bigArrayOffset + bigArrayCount)
            {
                for (int i = 0; i < starts.Count; i++)
                {
                    if (bigArray[offset] != smallArray[offset - starts[i]])
                    {
                        // Remove starts[i] from the list. 
                        starts.RemoveAt(i);
                        i--;
                    }
                    else if (offset - starts[i] == smallArray.Length - 1)
                    {
                        // Found a match. 
                        return starts[i];
                    }
                }
                if (bigArray[offset] == smallArray[0] &&
                    offset <= (bigArrayOffset + bigArrayCount - smallArray.Length))
                {
                    if (smallArray.Length > 1)
                        // Add the start to the list. 
                        starts.Add(offset);
                    else
                        // Found a match. 
                        return offset;
                }
                offset++;
            }
            return -1;
        }

        //------------------------------------------------------------------------------------
        //	This method simulates the classic C string function 'strtok' (and 'wcstok').
        //	Note that the .NET string 'Split' method cannot be used to simulate 'strtok' since
        //	it doesn't allow changing the delimiters between each token retrieval.
        //------------------------------------------------------------------------------------

        internal static string StrTok(string stringtotokenize, string delimiters)
        {
            if (stringtotokenize != null)
            {
                activestring = stringtotokenize;
                activeposition = -1;
            }

            //the stringtotokenize was never set:
            if (activestring == null)
                return null;

            //all tokens have already been extracted:
            if (activeposition == activestring.Length)
                return null;

            //bypass delimiters:
            activeposition++;
            while (activeposition < activestring.Length && delimiters.IndexOf(activestring[activeposition]) > -1)
            {
                activeposition++;
            }

            //only delimiters were left, so return null:
            if (activeposition == activestring.Length)
                return null;

            //get starting position of string to return:
            int startingposition = activeposition;

            //read until next delimiter:
            do
            {
                activeposition++;
            } while (activeposition < activestring.Length && delimiters.IndexOf(activestring[activeposition]) == -1);

            return activestring.Substring(startingposition, activeposition - startingposition);
        }
    }
}
