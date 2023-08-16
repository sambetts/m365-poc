
import { AppContentManager } from '../../engine/AppContentManager';
import { getStorageConfigFromAPI } from '../../api/ApiCalls';
import '../NavMenu.css';
import React from 'react';
import { ApiAppContentLoader } from '../../engine/IAppContentLoader';
import { getTeamsMeetingDetails } from '../../engine/TeamsMeetingUrlParser';
import { TeamsCallContainer } from './TeamsCallContainer';

export const MainDisplay: React.FC<{}> = () => {

  const [currentPlayListItem, setCurrentPlayListItem] = React.useState<PlayListItem | null>(null);
  const [currentTeamsMeetingDetails, setCurrentTeamsMeetingDetails] = React.useState<TeamsMeetingDetails | null>(null);
  const [serviceConfiguration, setServiceConfiguration] = React.useState<ServiceConfiguration | null>(null);

  // Use refs for setInterval callback
  const currentTeamsMeetingDetailsRef = React.useRef<TeamsMeetingDetails | null>(null);

  React.useEffect(() => {

    getStorageConfigFromAPI()
      .then((storageConfigInfo: ServiceConfiguration) => {
        console.log('Got service config from site API');
        setServiceConfiguration(storageConfigInfo);

        const contentManager = new AppContentManager(storageConfigInfo.clientLocationInfo.name, 5000, new ApiAppContentLoader());
        contentManager.start((pli: PlayListItem) => {

          // New item to play. May or may not be a meeting.
          setCurrentPlayListItem(pli);

          const meeting = getTeamsMeetingDetails(pli?.url);
          if (meeting) {
            if (!currentTeamsMeetingDetailsRef.current) {
              console.info("Have a meeting to start");
              currentTeamsMeetingDetailsRef.current = meeting;
              setCurrentTeamsMeetingDetails(meeting);
            }
          }
          else {
            if (currentTeamsMeetingDetailsRef.current) {

              // Now have no meeting, but we did before. Clean-up meeting.
              console.warn("Meeting time-up!");
              currentTeamsMeetingDetailsRef.current = null;
              setCurrentTeamsMeetingDetails(null);
            }
          }
        })
      });

  }, []);

  return (
    <div>
      <h3>Now Showing on {serviceConfiguration?.clientLocationInfo.name}</h3>
    
      {serviceConfiguration ?
        (
          <div>
            <p>You are connected via {serviceConfiguration.clientLocationInfo.description}</p>
            {currentTeamsMeetingDetails ?
              <>
                <TeamsCallContainer config={serviceConfiguration} meeting={currentTeamsMeetingDetails} />
              </>
              :
              <>
                {currentPlayListItem?.url ?
                  <>
                    <iframe src={currentPlayListItem.url} id='webBrowser'>
                    </iframe>
                  </>
                  :
                  <div>Nothing to play right now...</div>
                }
              </>
            }

          </div>
        )
        : <div>Loading</div>
      }
    </div>
  );
};
