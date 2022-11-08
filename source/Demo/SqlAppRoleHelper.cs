using System.Data;
using Microsoft.Data.SqlClient;
using NHibernate;

namespace Demo;

/// <summary>
/// Helper for setting up app role and cookies
/// </summary>
public static class SqlAppRoleHelper {
    /// <summary>
    /// Setup app role that should be used in the current connection
    /// </summary>
    /// <param name="session"><see cref="ISession"/> that has the connection that should be updated</param>
    /// <param name="role">SQL app role that should be used for the connection</param>
    /// <param name="password">SQL app role's password</param>
    /// <remarks>Can't be called inside a transaction and if reusing the connection, then must change connection release mode to on_close</remarks>
    /// <returns>Returns the cookie that identifies the role that has been set</returns>
    public static  Task<byte[]> SetApplicationRoleAsync(ISession session, string role, string password) => 
        SetApplicationRoleAsync(( session.Connection as SqlConnection )!, role, password);

    /// <summary>
    /// Setup app role that should be used in the current connection
    /// </summary>
    /// <param name="cnn">Open <see cref="SqlConnection"/> for a database</param>
    /// <param name="role">SQL app role that should be used for the connection</param>
    /// <param name="password">SQL app role's password</param>
    /// <returns></returns>
    public static async Task<byte[]> SetApplicationRoleAsync(SqlConnection cnn, string role, string password) {
        await using var cmd = cnn.CreateCommand( );
        if( cmd is null ) {
            throw new Exception("Unable to set application role");
        }
        //tran.Enlist(cmd);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_setapprole";
        cmd.Parameters.Add(new SqlParameter("@rolename", role));
        cmd.Parameters.Add(new SqlParameter("@password", password));
        cmd.Parameters.Add(new SqlParameter("@fCreateCookie", true));
        cmd.Parameters.Add(new SqlParameter {
                                                ParameterName = "@cookie",
                                                DbType = DbType.Binary,
                                                Direction = ParameterDirection.Output,
                                                Size = 8000
                                            });

        await cmd.ExecuteNonQueryAsync( );
        return (byte[])cmd.Parameters["@cookie"].Value;
    }

    /// <summary>
    /// Clear the sql app role that has been used
    /// </summary>
    /// <param name="session"><see cref="ISession"/> that has the connection that should be updated</param>
    /// <param name="cookie">cookie that identifies the current role</param>
    /// <remarks>Can't be called inside a transaction and if reusing the connection, then must change connection release mode to on_close</remarks>
    public static Task UnsetApplicationRoleAsync(ISession session, byte[] cookie) => UnsetApplicationRoleAsync(( session.Connection as SqlConnection )!, cookie);
    

    /// <summary>
    /// Clear the sql app role that has been used
    /// </summary>
    /// <param name="cnn">Open <see cref="SqlConnection"/> for a database</param>
    /// <param name="cookie">cookie that identifies the current role</param>
    /// <remarks>Can't be called inside a transaction and if reusing the connection, then must change connection release mode to on_close</remarks>
    public static async Task UnsetApplicationRoleAsync(SqlConnection cnn,  byte[] cookie) {
        await using var cmd = cnn.CreateCommand( );
        if( cmd is null ) {
            throw new Exception("Unable to unset application role");
        }
        cmd.CommandText = "sp_unsetapprole";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@cookie", cookie));
        await cmd.ExecuteNonQueryAsync( );
    }
    
}
