

interface ServiceConfiguration {
  clientLocationInfo: LocationInfo;
  acsEndpointVal: string;
  acsAccessKeyVal: string;
}

interface PlayListItem {
  scope: string;
  url: string;
  start: Date
  end: Date
}

interface TeamsMeetingDetails {
  joinUrl: string,
  activateAcsClientWebCam: boolean;
  activateAcsClientMic: boolean;
}

interface LocationInfo {
  name: string;
  description: string;
}
