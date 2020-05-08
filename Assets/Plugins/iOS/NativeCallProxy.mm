#import <Foundation/Foundation.h>
#import "NativeCallProxy.h"


@implementation FrameworkLibAPI

id<NativeCallsProtocol> api = NULL;
+(void) registerAPIforNativeCalls:(id<NativeCallsProtocol>) aApi
{
    api = aApi;
}

@end


extern "C" {

    void showNativeApp() {
        return [api showHostMainWindow];
        
    }

    void giveDataFromAR(const char* mapID, const char* activity){
        return [api giveDataFromAR:[NSString stringWithUTF8String:mapID] act:[NSString stringWithUTF8String:activity]];
        
    }
}