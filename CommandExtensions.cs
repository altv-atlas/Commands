using System.Web;
using AltV.Icarus.Commands.Interfaces;

namespace AltV.Icarus.Commands;

public static class CommandExtensions
{
    public static dynamic[ ]? GetCommandArgs( this IAsyncCommand asyncCommand, string? arg, params Type[ ] types )
    {
        if( string.IsNullOrEmpty( arg ) )
        {
            return null;
        }

        var args = arg.Split( " " );

        if( args.Length < types.Length )
        {
            return null;
        }

        // FIX FOR LAST ARGUMENT BEING A STRING WITH MULTIPLE WORDS
        if( args.Length > types.Length )
        {
            // if last argument isn't supposed to be a string then the user most likely entered some incorrect data
            if( types[ ^1 ] != typeof( string ) )
            {
                return null;
            }

            // temp var to store the string containing last args in
            var lastArg = "";

            // loop thru the args starting at the index of where the last argument is supposed to be
            for( var i = types.Length - 1; i < args.Length; i++ )
            {
                lastArg += $"{args[ i ]} "; // add it to the variable and a space at the end
            }

            lastArg = lastArg.Trim( ); // trim the last space off the end

            // temp variable to hold the new arguments in
            var newArgs = new string[ types.Length ];

            // loop thru old arguments
            for( var i = 0; i < args.Length; i++ )
            {
                // if the index is at the last expected argument index, push the previously created string of all the rest of the arguments to the array and quit the loop since we're done
                if( i == types.Length - 1 )
                {
                    newArgs[ i ] = lastArg;
                    break;
                }

                // push the old argument to the new arguments list if we haven't arrived at the end yet
                newArgs[ i ] = args[ i ];
            }

            // push new arguments to args variable.
            args = newArgs;
        }
        // END FIX MULTIPLE WORDS

        var result = new dynamic[ args.Length ];

        for( var i = 0; i < args.Length; i++ )
        {
            result[ i ] = args[ i ];

            if( types[ i ] == typeof( int ) )
            {
                if( int.TryParse( args[ i ], out var tmp ) )
                {
                    result[ i ] = tmp;
                }
                else
                {
                    result = null;
                    break;
                }
            }

            if( types[ i ] == typeof( uint ) )
            {
                if( uint.TryParse( args[ i ], out var tmp ) )
                {
                    result[ i ] = tmp;
                }
                else
                {
                    result = null;
                    break;
                }
            }

            if( types[ i ] == typeof( byte ) )
            {
                if( byte.TryParse( args[ i ], out var tmp ) )
                {
                    result[ i ] = tmp;
                }
                else
                {
                    result = null;
                    break;
                }
            }

            if( types[ i ] == typeof( sbyte ) )
            {
                if( sbyte.TryParse( args[ i ], out var tmp ) )
                {
                    result[ i ] = tmp;
                }
                else
                {
                    result = null;
                    break;
                }
            }

            if( types[ i ] == typeof( ushort ) )
            {
                if( ushort.TryParse( args[ i ], out var tmp ) )
                {
                    result[ i ] = tmp;
                }
                else
                {
                    result = null;
                    break;
                }
            }

            if( types[ i ] == typeof( short ) )
            {
                if( short.TryParse( args[ i ], out var tmp ) )
                {
                    result[ i ] = tmp;
                }
                else
                {
                    result = null;
                    break;
                }
            }

            if( types[ i ] == typeof( ulong ) )
            {
                if( ulong.TryParse( args[ i ], out var tmp ) )
                {
                    result[ i ] = tmp;
                }
                else
                {
                    result = null;
                    break;
                }
            }

            if( types[ i ] == typeof( long ) )
            {
                if( long.TryParse( args[ i ], out var tmp ) )
                {
                    result[ i ] = tmp;
                }
                else
                {
                    result = null;
                    break;
                }
            }

            if( types[ i ] == typeof( string ) )
            {
                result[ i ] = HttpUtility.HtmlEncode( args[ i ] );
            }

            if( types[ i ] == typeof( float ) )
            {
                if( float.TryParse( args[ i ], out var tmp ) )
                {
                    result[ i ] = tmp;
                }
                else
                {
                    result = null;
                    break;
                }
            }

            if( types[ i ] == typeof( bool ) )
            {
                if( bool.TryParse( args[ i ], out var tmp ) )
                {
                    result[ i ] = tmp;
                }
                else
                {
                    result = null;
                    break;
                }
            }
        }

        return result != null && result.Length == types.Length ? result : null;
    }
}