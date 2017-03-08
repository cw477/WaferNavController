import org.codehaus.jackson.map.ObjectMapper;
import org.codehaus.jackson.type.TypeReference;
import org.eclipse.paho.client.mqttv3.*;

import java.io.IOException;
import java.time.LocalDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

public class WaferNavPubSub {
    public static final String BROKER_URL = "tcp://iot.eclipse.org:1883";
    public static final String SUB_TOPIC = "wafernav/location_requests";
    public static final String PUB_TOPIC = "wafernav/location_data";

    private static final String CLIENT_ID = UUID.randomUUID().toString();
    private MqttClient mqttClient;
    private Map<Integer, String> mockDatabase;

    public WaferNavPubSub() throws InterruptedException, IOException {
        mockDatabase = new HashMap<>();
        mockDatabase.put(123, "abc");
        mockDatabase.put(456, "xyz");
        mockDatabase.put(12345, "somewhere");

        //testingJsonParsing();

        System.out.println(CLIENT_ID);
        try {
            mqttClient = new MqttClient(BROKER_URL, CLIENT_ID);
            mqttClient.setCallback(new SubscribeCallback());
            mqttClient.connect();

            mqttClient.subscribe(SUB_TOPIC);
            System.out.println("Subscribed to " + SUB_TOPIC);
        }
        catch (MqttException e) {
            e.printStackTrace();
            System.exit(1);
        }
    }

    private void testingJsonParsing() throws IOException {
        Map<Integer, String> map = new HashMap<>();
        map.put(123, "xyz");

        String jsonString = new ObjectMapper().writeValueAsString(map);
        System.out.println(jsonString);

        ObjectMapper objectMapper = new ObjectMapper();
        Map<Integer, String> resultMap = objectMapper.readValue(jsonString, new TypeReference<HashMap<Integer, String>>() {
        });
        System.out.println(resultMap);
    }

    private void pubLocation(MqttMessage mqttMessage) {
        try {
            // Process mqtt message to get desired ID
            String jsonString = mqttMessage.toString();
            ObjectMapper objectMapper = new ObjectMapper();
            Map<String, Object> resultMap = objectMapper.readValue(jsonString, new TypeReference<HashMap<String, Object>>() {
            });
            int id = (int) resultMap.get("id"); // e.g. 123

            // Get location data to return from "database"
            String loc = mockDatabase.get(id); // e.g. "xyz"

            // create json string to send back, e.g. {"id":123, "loc":"xyz"}
            Map<String, Object> returnMap = new HashMap<>();
            returnMap.put("id", id);
            returnMap.put("loc", loc);
            String returnJsonString = new ObjectMapper().writeValueAsString(returnMap);

            // publish location info
            final MqttTopic topic = mqttClient.getTopic(PUB_TOPIC);
            topic.publish(new MqttMessage(returnJsonString.toString().getBytes()));
        }
        catch (Exception e) {
            e.printStackTrace();
        }
    }

    private class SubscribeCallback implements MqttCallback {

        @Override
        public void connectionLost(Throwable cause) {
        }

        @Override
        public void messageArrived(String topic, MqttMessage mqttMessage) throws IOException, MqttException {
            System.out.println(LocalDateTime.now() + "  Message arrived.  Topic: " + topic + "  Message: '" + mqttMessage.toString() + "'");

            pubLocation(mqttMessage);
        }

        @Override
        public void deliveryComplete(IMqttDeliveryToken iMqttDeliveryToken) {
        }
    }

    public static void main(String[] args) throws InterruptedException, IOException {
        try {
            new WaferNavPubSub();
        }
        catch (Exception e) {
            e.printStackTrace();
        }
    }
}
