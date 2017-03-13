// Use the JDBC driver
import java.sql.*;
import com.microsoft.sqlserver.jdbc.*;

public class WaferNavController {

    // Connect to your database.
    // Replace server name, username, and password with your credentials
    public static void main(String[] args) {

        String connectionString = "jdbc:sqlserver://localhost;DatabaseName=wafer_nav;user=appuser;password=appuser;";

        // Declare the JDBC objects.
        Connection connection = null;
        Statement statement = null;
        ResultSet resultSet = null;
        PreparedStatement prepsInsertProduct = null;
        try {
            connection = DriverManager.getConnection(connectionString);

//            String insertSql = "INSERT INTO wn.active_bib VALUES "
//                    + "(984545945);";
//
//            prepsInsertProduct = connection.prepareStatement(
//                    insertSql,
//                    Statement.RETURN_GENERATED_KEYS);
//            prepsInsertProduct.execute();
//
//            resultSet = prepsInsertProduct.getGeneratedKeys();
//            while (resultSet.next()) {
//                System.out.println("Generated: " + resultSet.getString(1));
//            }

            String query = "select id from wn.active_bib";
            statement = connection.createStatement();
            resultSet = statement.executeQuery(query);
            while (resultSet.next()) {
                int id = resultSet.getInt("id");
                System.out.println("Found record: " + id);
            }
        }
        catch (Exception e) {
            e.printStackTrace();
        }
        finally {
            if (prepsInsertProduct != null) try { prepsInsertProduct.close(); } catch(Exception e) {}
            if (resultSet != null) try { resultSet.close(); } catch(Exception e) {}
            if (statement != null) try { statement.close(); } catch(Exception e) {}
            if (connection != null) try { connection.close(); } catch(Exception e) {}
        }
    }
}