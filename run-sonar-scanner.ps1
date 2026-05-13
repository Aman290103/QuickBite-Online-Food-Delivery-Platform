# Install SonarScanner if not already installed
dotnet tool install --global dotnet-sonarscanner

# NOTE: Before running this script, ensure your SonarQube container is running 
# (docker-compose -f docker-compose.sonar.yml up -d)
# and you have generated a token in SonarQube UI at http://localhost:9000

$SONAR_TOKEN = "squ_4be14aacf73f15bbb28562a0d1d0da1362e64154"

# Start the SonarQube Scanner
dotnet sonarscanner begin /k:"QuickBite" /d:sonar.host.url="http://localhost:9000" /d:sonar.login="$SONAR_TOKEN"

# Build the entire solution
dotnet build QuickBite.sln

# End the SonarQube Scanner and upload results
$env:JAVA_HOME="C:\Program Files\Eclipse Adoptium\jdk-17.0.19.10-hotspot"
dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"
